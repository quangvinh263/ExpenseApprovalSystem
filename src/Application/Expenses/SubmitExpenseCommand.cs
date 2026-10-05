using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record SubmitExpenseCommand(
    Guid ExpenseRequestId,
    Guid UserId) : IRequest<Guid>;
