using ExpenseApproval.Domain.Entities;

namespace ExpenseApproval.Application.Abstractions;

public interface IApplicationDbContext
{
    Task<ExpenseRequest?> GetExpenseRequestAsync(
        Guid expenseRequestId,
        CancellationToken cancellationToken = default);

    void AddExpenseRequest(ExpenseRequest expenseRequest);
    void UpdateExpenseRequest(ExpenseRequest expenseRequest);
    void AddApprovalHistory(ApprovalHistory approvalHistory);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
