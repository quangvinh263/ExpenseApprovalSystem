using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using ExpenseApproval.Domain.Workflow;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class ApproveExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ApproveExpenseCommand, Guid>
{
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
        var approvedExpenseRequest = ExpenseWorkflow.Approve(
            expenseRequest,
            now,
            Guid.NewGuid().ToByteArray());

        dbContext.UpdateExpenseRequest(approvedExpenseRequest, request.RowVersion);
        dbContext.AddApprovalHistory(new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = approvedExpenseRequest.Id,
            ActionByUserId = request.ApproverId,
            Action = ExpenseWorkflow.ApproveAction,
            FromStatus = ExpenseWorkflow.PendingApproval,
            ToStatus = ExpenseWorkflow.Approved,
            Comment = request.Comment,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return approvedExpenseRequest.Id;
    }
}
