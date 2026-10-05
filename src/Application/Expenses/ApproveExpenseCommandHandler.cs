using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class ApproveExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ApproveExpenseCommand, Guid>
{
    private const string PendingApprovalStatus = "PendingApproval";
    private const string ApprovedStatus = "Approved";
    private const string ApproveAction = "Approve";

    public async Task<Guid> Handle(
        ApproveExpenseCommand request,
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
        var approvedExpenseRequest = expenseRequest with
        {
            Status = ApprovedStatus,
            CurrentApproverRole = null,
            UpdatedAt = now,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        dbContext.UpdateExpenseRequest(approvedExpenseRequest, request.RowVersion);
        dbContext.AddApprovalHistory(new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = approvedExpenseRequest.Id,
            ActionByUserId = request.ApproverId,
            Action = ApproveAction,
            FromStatus = PendingApprovalStatus,
            ToStatus = ApprovedStatus,
            Comment = request.Comment,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return approvedExpenseRequest.Id;
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
                "The user is not authorized to approve this expense request.");
        }
    }
}
