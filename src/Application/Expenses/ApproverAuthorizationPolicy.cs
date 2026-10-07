using ExpenseApproval.Domain.Entities;

namespace ExpenseApproval.Application.Expenses;

internal static class ApproverAuthorizationPolicy
{
    public static void EnsureCanAct(ExpenseRequest expenseRequest, User? approver)
    {
        if (approver is null ||
            !approver.IsActive ||
            !string.Equals(
                approver.Role,
                expenseRequest.CurrentApproverRole,
                StringComparison.Ordinal) ||
            approver.DepartmentId != expenseRequest.DepartmentId ||
            approver.Id == expenseRequest.RequesterId)
        {
            throw new UnauthorizedAccessException(
                "The user is not authorized to act on this expense request.");
        }
    }
}
