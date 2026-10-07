using ExpenseApproval.Domain.Entities;

namespace ExpenseApproval.Domain.Workflow;

public static class ExpenseWorkflow
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Paid = "Paid";

    public const string Employee = "Employee";
    public const string TeamLead = "TeamLead";
    public const string Manager = "Manager";
    public const string Accountant = "Accountant";
    public const string Admin = "Admin";

    public const string SubmitAction = "Submit";
    public const string ApproveAction = "Approve";
    public const string RejectAction = "Reject";
    public const string MarkPaidAction = "MarkPaid";

    public static readonly IReadOnlySet<string> Categories =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Travel", "Meal", "Hotel", "Supplies", "Other"
        };

    public static ExpenseRequest Submit(
        ExpenseRequest expenseRequest,
        string approverRole,
        DateTimeOffset occurredAt,
        byte[] rowVersion)
    {
        EnsureStatus(expenseRequest, Draft);

        var submitted = expenseRequest with
        {
            Status = Submitted,
            UpdatedAt = occurredAt,
            RowVersion = rowVersion
        };

        return submitted with
        {
            Status = PendingApproval,
            CurrentApproverRole = approverRole
        };
    }

    public static ExpenseRequest Approve(
        ExpenseRequest expenseRequest,
        DateTimeOffset occurredAt,
        byte[] rowVersion)
    {
        EnsureStatus(expenseRequest, PendingApproval);

        return expenseRequest with
        {
            Status = Approved,
            CurrentApproverRole = null,
            UpdatedAt = occurredAt,
            RowVersion = rowVersion
        };
    }

    public static ExpenseRequest Reject(
        ExpenseRequest expenseRequest,
        DateTimeOffset occurredAt,
        byte[] rowVersion)
    {
        EnsureStatus(expenseRequest, PendingApproval);

        return expenseRequest with
        {
            Status = Rejected,
            CurrentApproverRole = null,
            UpdatedAt = occurredAt,
            RowVersion = rowVersion
        };
    }

    public static ExpenseRequest MarkPaid(
        ExpenseRequest expenseRequest,
        DateTimeOffset occurredAt,
        byte[] rowVersion)
    {
        EnsureStatus(expenseRequest, Approved);

        return expenseRequest with
        {
            Status = Paid,
            CurrentApproverRole = null,
            UpdatedAt = occurredAt,
            RowVersion = rowVersion
        };
    }

    public static void EnsureStatus(ExpenseRequest expenseRequest, string expectedStatus)
    {
        if (!string.Equals(expenseRequest.Status, expectedStatus, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expense request '{expenseRequest.Id}' cannot transition from '{expenseRequest.Status}'.");
        }
    }
}
