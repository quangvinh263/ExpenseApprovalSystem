namespace ExpenseApproval.Domain.Entities;

public record ExpenseRequest
{
    public Guid Id { get; init; }
    public Guid RequesterId { get; init; }
    public Guid DepartmentId { get; init; }
    public decimal Amount { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? ReceiptUrl { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? CurrentApproverRole { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public byte[] RowVersion { get; init; } = [];

    public User Requester { get; init; } = null!;
    public Department Department { get; init; } = null!;
    public ICollection<ApprovalHistory> ApprovalHistories { get; init; } = new List<ApprovalHistory>();
}
