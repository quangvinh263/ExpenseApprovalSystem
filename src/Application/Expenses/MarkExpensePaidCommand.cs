using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record MarkExpensePaidCommand(
    Guid ExpenseRequestId,
    Guid UserId) : IRequest<Guid>;
