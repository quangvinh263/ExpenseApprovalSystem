using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class RejectExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<RejectExpenseCommand, Guid>
{
    private const string PendingApprovalStatus = "PendingApproval";
    private const string RejectedStatus = "Rejected";
    private const string RejectAction = "Reject";

    public async Task<Guid> Handle(
        RejectExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ArgumentException(
                "A rejection comment is required.",
                nameof(request.Comment));
        }

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
                PendingApprovalStatus,
                StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(expenseRequest.CurrentApproverRole))
        {
            throw new InvalidOperationException(
                $"Expense request '{request.ExpenseRequestId}' is not pending approval.");
        }

        var approver = await dbContext.GetUserAsync(request.ApproverId, cancellationToken);
        ValidateApprover(expenseRequest, approver);

        var now = DateTimeOffset.UtcNow;
        var rejectedExpenseRequest = expenseRequest with
        {
            Status = RejectedStatus,
            CurrentApproverRole = null,
            UpdatedAt = now,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        dbContext.UpdateExpenseRequest(rejectedExpenseRequest, request.RowVersion);
        dbContext.AddApprovalHistory(new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = rejectedExpenseRequest.Id,
            ActionByUserId = request.ApproverId,
            Action = RejectAction,
            FromStatus = PendingApprovalStatus,
            ToStatus = RejectedStatus,
            Comment = request.Comment.Trim(),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return rejectedExpenseRequest.Id;
    }

    private static void ValidateApprover(ExpenseRequest expenseRequest, User? approver)
    {
        if (approver is null ||
            !approver.IsActive ||
            !string.Equals(
                approver.Role,
                expenseRequest.CurrentApproverRole,
                StringComparison.Ordinal) ||
            approver.DepartmentId != expenseRequest.DepartmentId ||
            approver.Id == expenseRequest.RequesterId)
        {
            throw new UnauthorizedAccessException(
                "The user is not authorized to reject this expense request.");
        }
    }
}
