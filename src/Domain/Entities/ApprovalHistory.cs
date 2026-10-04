namespace ExpenseApproval.Domain.Entities;

public record ApprovalHistory
{
    public Guid Id { get; init; }
    public Guid ExpenseRequestId { get; init; }
    public Guid ActionByUserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string FromStatus { get; init; } = string.Empty;
    public string ToStatus { get; init; } = string.Empty;
    public string? Comment { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public ExpenseRequest ExpenseRequest { get; init; } = null!;
    public User ActionByUser { get; init; } = null!;
}
