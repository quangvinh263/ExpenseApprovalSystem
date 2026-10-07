using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using ExpenseApproval.Domain.Workflow;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class SubmitExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SubmitExpenseCommand, Guid>
{
    private const decimal TeamLeadApprovalLimit = 10_000_000m;

    public async Task<Guid> Handle(
        SubmitExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var expenseRequest = await dbContext.GetExpenseRequestAsync(
            request.ExpenseRequestId,
            cancellationToken);

        if (expenseRequest is null)
        {
            throw new KeyNotFoundException(
                $"Expense request '{request.ExpenseRequestId}' was not found.");
        }

        if (!string.Equals(expenseRequest.Status, ExpenseWorkflow.Draft, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expense request '{request.ExpenseRequestId}' is not in Draft status.");
        }

        var requester = await dbContext.GetUserAsync(request.UserId, cancellationToken);
        if (requester is null || !requester.IsActive ||
            requester.DepartmentId != expenseRequest.DepartmentId)
        {
            throw new UnauthorizedAccessException(
                "The requester is not active or does not belong to the expense department.");
        }

        var department = await dbContext.GetDepartmentAsync(
            expenseRequest.DepartmentId,
            cancellationToken);
        if (department is null || !department.IsActive)
        {
            throw new InvalidOperationException(
                "The expense department does not exist or is inactive.");
        }

        if (expenseRequest.Amount <= 0 ||
            expenseRequest.Amount > 9_999_999_999_999_999.99m ||
            decimal.Round(expenseRequest.Amount, 2) != expenseRequest.Amount)
        {
            throw new ArgumentException("Expense amount must be positive and have at most two decimals.");
        }

        var validCategories = new[] { "Travel", "Meal", "Hotel", "Supplies", "Other" };
        if (!validCategories.Contains(expenseRequest.Category, StringComparer.Ordinal))
        {
            throw new ArgumentException("Expense category is invalid.");
        }

        if (string.IsNullOrWhiteSpace(expenseRequest.Reason))
        {
            throw new ArgumentException("Expense reason is required.");
        }

        var currentApproverRole = expenseRequest.Amount <= TeamLeadApprovalLimit
            ? ExpenseWorkflow.TeamLead
            : ExpenseWorkflow.Manager;
        var now = DateTimeOffset.UtcNow;

        var submittedExpenseRequest = ExpenseWorkflow.Submit(
            expenseRequest,
            currentApproverRole,
            now,
            Guid.NewGuid().ToByteArray());

        dbContext.UpdateExpenseRequest(submittedExpenseRequest, request.RowVersion);
        dbContext.AddApprovalHistory(new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = submittedExpenseRequest.Id,
            ActionByUserId = request.UserId,
            Action = ExpenseWorkflow.SubmitAction,
            FromStatus = ExpenseWorkflow.Draft,
            ToStatus = ExpenseWorkflow.PendingApproval,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return submittedExpenseRequest.Id;
    }
}
