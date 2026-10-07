using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using ExpenseApproval.Domain.Workflow;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed class CreateExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateExpenseCommand, Guid>
{

    public async Task<Guid> Handle(
        CreateExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var requester = await dbContext.GetUserAsync(
            request.RequesterId,
            cancellationToken);

        if (requester is null ||
            !requester.IsActive ||
            requester.DepartmentId != request.DepartmentId)
        {
            throw new UnauthorizedAccessException(
                "The requester is not active or does not belong to the selected department.");
        }

        var department = await dbContext.GetDepartmentAsync(
            request.DepartmentId,
            cancellationToken);

        if (department is null || !department.IsActive)
        {
            throw new ArgumentException(
                "The selected department does not exist or is inactive.",
                nameof(request.DepartmentId));
        }

        if (request.Amount <= 0 ||
            request.Amount > 9_999_999_999_999_999.99m ||
            decimal.Round(request.Amount, 2) != request.Amount)
        {
            throw new ArgumentException(
                "Expense amount must be positive and have at most two decimals.",
                nameof(request.Amount));
        }

        if (!ExpenseWorkflow.Categories.Contains(request.Category))
        {
            throw new ArgumentException(
                "Expense category is invalid.",
                nameof(request.Category));
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException(
                "Expense reason is required.",
                nameof(request.Reason));
        }

        var now = DateTimeOffset.UtcNow;
        var expenseRequest = new ExpenseRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = request.RequesterId,
            DepartmentId = request.DepartmentId,
            Amount = request.Amount,
            Category = request.Category,
            Reason = request.Reason.Trim(),
            Status = ExpenseWorkflow.Draft,
            CurrentApproverRole = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.AddExpenseRequest(expenseRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return expenseRequest.Id;
    }
}
