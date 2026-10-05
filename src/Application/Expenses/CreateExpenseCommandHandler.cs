using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class CreateExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateExpenseCommand, Guid>
{
    private const string DraftStatus = "Draft";
    private const string InitialApproverRole = "TeamLead";

    public async Task<Guid> Handle(
        CreateExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var expenseRequest = new ExpenseRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = request.RequesterId,
            DepartmentId = request.DepartmentId,
            Amount = request.Amount,
            Category = request.Category,
            Reason = request.Reason,
            Status = DraftStatus,
            CurrentApproverRole = InitialApproverRole,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.AddExpenseRequest(expenseRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return expenseRequest.Id;
    }
}
