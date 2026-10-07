using ExpenseApproval.Application.Abstractions;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class GetExpenseQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetExpenseQuery, ExpenseDetails>
{
    public async Task<ExpenseDetails> Handle(
        GetExpenseQuery request,
        CancellationToken cancellationToken)
    {
        var expense = await dbContext.GetExpenseRequestAsync(
            request.ExpenseRequestId,
            cancellationToken);

        if (expense is null)
        {
            throw new KeyNotFoundException(
                $"Expense request '{request.ExpenseRequestId}' was not found.");
        }

        return new ExpenseDetails(
            expense.Id,
            expense.RequesterId,
            expense.DepartmentId,
            expense.Amount,
            expense.Category,
            expense.Reason,
            expense.Status,
            expense.CurrentApproverRole,
            expense.CreatedAt,
            expense.UpdatedAt,
            expense.RowVersion);
    }
}
