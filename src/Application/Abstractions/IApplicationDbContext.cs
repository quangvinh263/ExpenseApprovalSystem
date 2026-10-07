using ExpenseApproval.Domain.Entities;

namespace ExpenseApproval.Application.Abstractions;

public interface IApplicationDbContext
{
    Task<ExpenseRequest?> GetExpenseRequestAsync(
        Guid expenseRequestId,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<Department?> GetDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalHistory>> GetApprovalHistoriesAsync(
        Guid expenseRequestId,
        CancellationToken cancellationToken = default);

    void AddExpenseRequest(ExpenseRequest expenseRequest);
    void AddUser(User user);
    void UpdateExpenseRequest(ExpenseRequest expenseRequest, byte[] originalRowVersion);
    void AddApprovalHistory(ApprovalHistory approvalHistory);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
