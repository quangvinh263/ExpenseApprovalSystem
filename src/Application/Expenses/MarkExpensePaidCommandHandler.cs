using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class MarkExpensePaidCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<MarkExpensePaidCommand, Guid>
{
    private const string ApprovedStatus = "Approved";
    private const string PaidStatus = "Paid";
    private const string MarkPaidAction = "MarkPaid";

    public async Task<Guid> Handle(
        MarkExpensePaidCommand request,
        CancellationToken cancellationToken)
    {
        var expenseRequest = await dbContext.GetExpenseRequestAsync(
            request.ExpenseRequestId,
            cancellationToken);

        if (expenseRequest is null)
        {
            throw new KeyNotFoundException(
                $"Expense request '{request.ExpenseRequestId}' was not found.");
        }

        if (!string.Equals(
                expenseRequest.Status,
                ApprovedStatus,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expense request '{request.ExpenseRequestId}' is not approved.");
        }

        var user = await dbContext.GetUserAsync(request.UserId, cancellationToken);

        if (user is null ||
            !user.IsActive ||
            (!string.Equals(user.Role, "Accountant", StringComparison.Ordinal) &&
             !string.Equals(user.Role, "Admin", StringComparison.Ordinal)))
        {
            throw new UnauthorizedAccessException(
                "The user is not authorized to mark this expense request as paid.");
        }

        var now = DateTimeOffset.UtcNow;
        var paidExpenseRequest = expenseRequest with
        {
            Status = PaidStatus,
            CurrentApproverRole = null,
            UpdatedAt = now
        };

        dbContext.UpdateExpenseRequest(paidExpenseRequest);
        dbContext.AddApprovalHistory(new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = paidExpenseRequest.Id,
            ActionByUserId = request.UserId,
            Action = MarkPaidAction,
            FromStatus = ApprovedStatus,
            ToStatus = PaidStatus,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return paidExpenseRequest.Id;
    }
}
