namespace ExpenseApproval.Domain.Entities;

public record Department
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal MonthlyBudget { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }

    public ICollection<User> Users { get; init; } = new List<User>();
    public ICollection<ExpenseRequest> ExpenseRequests { get; init; } = new List<ExpenseRequest>();
}
