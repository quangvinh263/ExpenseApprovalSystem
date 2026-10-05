using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class SubmitExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SubmitExpenseCommand, Guid>
{
    private const string DraftStatus = "Draft";
    private const string PendingApprovalStatus = "PendingApproval";
    private const string SubmitAction = "Submit";
    private const decimal TeamLeadApprovalLimit = 10_000_000m;

    public async Task<Guid> Handle(
        SubmitExpenseCommand request,
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

        if (!string.Equals(expenseRequest.Status, DraftStatus, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expense request '{request.ExpenseRequestId}' is not in Draft status.");
        }

        var currentApproverRole = expenseRequest.Amount <= TeamLeadApprovalLimit
            ? "TeamLead"
            : "Manager";
        var now = DateTimeOffset.UtcNow;

        var submittedExpenseRequest = expenseRequest with
        {
            Status = PendingApprovalStatus,
            CurrentApproverRole = currentApproverRole,
            UpdatedAt = now
        };

        dbContext.UpdateExpenseRequest(submittedExpenseRequest);
        dbContext.AddApprovalHistory(new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = submittedExpenseRequest.Id,
            ActionByUserId = request.UserId,
            Action = SubmitAction,
            FromStatus = DraftStatus,
            ToStatus = PendingApprovalStatus,
            CreatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return submittedExpenseRequest.Id;
    }
}
