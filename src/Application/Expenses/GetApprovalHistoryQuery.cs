using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record GetApprovalHistoryQuery(Guid ExpenseRequestId)
    : IRequest<IReadOnlyList<ApprovalHistory>>;

public sealed class GetApprovalHistoryQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetApprovalHistoryQuery, IReadOnlyList<ApprovalHistory>>
{
    public async Task<IReadOnlyList<ApprovalHistory>> Handle(
        GetApprovalHistoryQuery request,
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

        return await dbContext.GetApprovalHistoriesAsync(
            request.ExpenseRequestId,
            cancellationToken);
    }
}
