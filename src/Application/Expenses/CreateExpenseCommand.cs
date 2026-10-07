using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record CreateExpenseCommand(
    Guid RequesterId,
    Guid DepartmentId,
    decimal Amount,
    string Category,
    string Reason) : IRequest<Guid>;
