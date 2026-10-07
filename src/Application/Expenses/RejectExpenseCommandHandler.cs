using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using ExpenseApproval.Domain.Workflow;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class RejectExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<RejectExpenseCommand, Guid>
{
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
                ExpenseWorkflow.PendingApproval,
                StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(expenseRequest.CurrentApproverRole))
        {
            throw new InvalidOperationException(
                $"Expense request '{request.ExpenseRequestId}' is not pending approval.");
        }

        var approver = await dbContext.GetUserAsync(request.ApproverId, cancellationToken);
        ApproverAuthorizationPolicy.EnsureCanAct(expenseRequest, approver);

        var now = DateTimeOffset.UtcNow;
        var rejectedExpenseRequest = ExpenseWorkflow.Reject(
            expenseRequest,
            now,
            Guid.NewGuid().ToByteArray());

        dbContext.UpdateExpenseRequest(rejectedExpenseRequest, request.RowVersion);
        dbContext.AddApprovalHistory(new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = rejectedExpenseRequest.Id,
            ActionByUserId = request.ApproverId,
            Action = ExpenseWorkflow.RejectAction,
            FromStatus = ExpenseWorkflow.PendingApproval,
            ToStatus = ExpenseWorkflow.Rejected,
            Comment = request.Comment.Trim(),
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return rejectedExpenseRequest.Id;
    }
}
