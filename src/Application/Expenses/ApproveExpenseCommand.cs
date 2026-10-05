using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record ApproveExpenseCommand(
    Guid ExpenseRequestId,
    Guid ApproverId,
    string? Comment) : IRequest<Guid>;
