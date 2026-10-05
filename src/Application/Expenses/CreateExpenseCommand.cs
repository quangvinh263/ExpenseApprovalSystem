using MediatR;

namespace ExpenseApproval.Application.Expenses;

public record CreateExpenseCommand(
    Guid RequesterId,
    Guid DepartmentId,
    decimal Amount,
    string Category,
    string Reason) : IRequest<Guid>;
