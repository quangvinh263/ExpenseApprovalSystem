namespace ExpenseApproval.Domain.Entities;

public record User
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTimeOffset CreatedAt { get; init; }

    public Department? Department { get; init; }
    public ICollection<ExpenseRequest> ExpenseRequests { get; init; } = new List<ExpenseRequest>();
    public ICollection<ApprovalHistory> ApprovalHistories { get; init; } = new List<ApprovalHistory>();
}
