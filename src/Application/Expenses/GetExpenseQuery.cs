using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record GetExpenseQuery(Guid ExpenseRequestId) : IRequest<ExpenseDetails>;

public sealed record ExpenseDetails(
    Guid Id,
    Guid RequesterId,
    Guid DepartmentId,
    decimal Amount,
    string Category,
    string Reason,
    string Status,
    string? CurrentApproverRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    byte[] RowVersion);
