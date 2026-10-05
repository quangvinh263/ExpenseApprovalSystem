using ExpenseApproval.Domain.Entities;

namespace ExpenseApproval.Application.Abstractions;

public interface IApplicationDbContext
{
    void AddExpenseRequest(ExpenseRequest expenseRequest);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
