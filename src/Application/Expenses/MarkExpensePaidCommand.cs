using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record MarkExpensePaidCommand(
    Guid ExpenseRequestId,
    Guid UserId,
    byte[] RowVersion) : IRequest<Guid>;
