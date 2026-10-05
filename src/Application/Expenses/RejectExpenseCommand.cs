using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record RejectExpenseCommand(
    Guid ExpenseRequestId,
    Guid ApproverId,
    string? Comment) : IRequest<Guid>;
