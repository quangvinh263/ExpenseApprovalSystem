using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Application.Expenses;
using ExpenseApproval.Domain.Entities;
using ExpenseApproval.Domain.Workflow;
using Xunit;

namespace ExpenseApproval.Application.Tests;

public sealed class UploadReceiptTests
{
    [Fact]
    public async Task Upload_updates_receipt_url_for_requester_with_valid_signature()
    {
        var expense = CreateExpense(ExpenseWorkflow.Draft);
        var db = new FakeDbContext(expense, CreateUser(expense.RequesterId, ExpenseWorkflow.Employee));
        var storage = new FakeStorage();
        await using var file = new MemoryStream([0xFF, 0xD8, 0xFF, 0x00]);

        var result = await CreateHandler(db, storage).Handle(
            new UploadReceiptCommand(
                expense.Id,
                expense.RequesterId,
                file,
                "receipt.jpg",
                "image/jpeg",
                file.Length),
            CancellationToken.None);

        Assert.Equal("/uploads/receipts/receipt.jpg", result);
        Assert.Equal(result, db.UpdatedExpense!.ReceiptUrl);
        Assert.Equal(1, db.SaveCount);
    }

    [Fact]
    public async Task Upload_rejects_mismatched_magic_bytes_without_writing()
    {
        var expense = CreateExpense(ExpenseWorkflow.Draft);
        var db = new FakeDbContext(expense, CreateUser(expense.RequesterId, ExpenseWorkflow.Employee));
        var storage = new FakeStorage();
        await using var file = new MemoryStream("%PDF-"u8.ToArray());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateHandler(db, storage).Handle(
                new UploadReceiptCommand(
                    expense.Id,
                    expense.RequesterId,
                    file,
                    "receipt.jpg",
                    "image/jpeg",
                    file.Length),
                CancellationToken.None));

        Assert.Equal(0, storage.UploadCount);
        Assert.Equal(0, db.SaveCount);
    }

    [Fact]
    public async Task Upload_rejects_actor_who_is_not_requester_or_elevated_same_department()
    {
        var expense = CreateExpense(ExpenseWorkflow.Draft);
        var actor = CreateUser(Guid.NewGuid(), ExpenseWorkflow.Employee);
        actor = actor with { DepartmentId = expense.DepartmentId };
        var db = new FakeDbContext(expense, actor);
        var storage = new FakeStorage();
        await using var file = new MemoryStream([0xFF, 0xD8, 0xFF]);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler(db, storage).Handle(
                new UploadReceiptCommand(
                    expense.Id,
                    actor.Id,
                    file,
                    "receipt.jpg",
                    "image/jpeg",
                    file.Length),
                CancellationToken.None));

        Assert.Equal(0, storage.UploadCount);
    }

    [Fact]
    public async Task Upload_rejects_file_larger_than_five_megabytes()
    {
        var expense = CreateExpense(ExpenseWorkflow.Draft);
        var db = new FakeDbContext(expense, CreateUser(expense.RequesterId, ExpenseWorkflow.Employee));
        var storage = new FakeStorage();
        await using var file = new MemoryStream(new byte[5 * 1024 * 1024 + 1]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateHandler(db, storage).Handle(
                new UploadReceiptCommand(
                    expense.Id,
                    expense.RequesterId,
                    file,
                    "receipt.pdf",
                    "application/pdf",
                    file.Length),
                CancellationToken.None));
    }

    [Fact]
    public async Task Upload_accepts_file_at_five_megabytes()
    {
        var expense = CreateExpense(ExpenseWorkflow.Draft);
        var db = new FakeDbContext(expense, CreateUser(expense.RequesterId, ExpenseWorkflow.Employee));
        var storage = new FakeStorage();
        var content = new byte[5 * 1024 * 1024];
        content[0] = 0xFF;
        content[1] = 0xD8;
        content[2] = 0xFF;
        await using var file = new MemoryStream(content);

        await CreateHandler(db, storage).Handle(
            new UploadReceiptCommand(
                expense.Id,
                expense.RequesterId,
                file,
                "receipt.jpg",
                "image/jpeg",
                file.Length),
            CancellationToken.None);

        Assert.Equal(1, storage.UploadCount);
    }

    [Fact]
    public async Task Upload_rejects_non_editable_workflow_status()
    {
        var expense = CreateExpense(ExpenseWorkflow.PendingApproval);
        var db = new FakeDbContext(expense, CreateUser(expense.RequesterId, ExpenseWorkflow.Employee));
        var storage = new FakeStorage();
        await using var file = new MemoryStream([0xFF, 0xD8, 0xFF]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler(db, storage).Handle(
                new UploadReceiptCommand(
                    expense.Id,
                    expense.RequesterId,
                    file,
                    "receipt.jpg",
                    "image/jpeg",
                    file.Length),
                CancellationToken.None));

        Assert.Equal(0, storage.UploadCount);
    }

    [Fact]
    public async Task Upload_cleans_up_file_when_persistence_fails()
    {
        var expense = CreateExpense(ExpenseWorkflow.Rejected);
        var db = new FakeDbContext(
            expense,
            CreateUser(expense.RequesterId, ExpenseWorkflow.Employee))
        {
            SaveException = new InvalidOperationException("database failure")
        };
        var storage = new FakeStorage();
        await using var file = new MemoryStream("%PDF-1.7"u8.ToArray());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler(db, storage).Handle(
                new UploadReceiptCommand(
                    expense.Id,
                    expense.RequesterId,
                    file,
                    "receipt.pdf",
                    "application/pdf",
                    file.Length),
                CancellationToken.None));

        Assert.Equal(1, storage.DeleteCount);
    }

    private static UploadReceiptCommandHandler CreateHandler(
        FakeDbContext db,
        FakeStorage storage) =>
        new(db, storage);

    private static ExpenseRequest CreateExpense(string status) =>
        new()
        {
            Id = Guid.NewGuid(),
            RequesterId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Amount = 100,
            Category = "Travel",
            Reason = "Business travel",
            Status = status,
            RowVersion = [1]
        };

    private static User CreateUser(Guid id, string role) =>
        new()
        {
            Id = id,
            FullName = "Test User",
            Email = $"{id}@local",
            Role = role,
            DepartmentId = Guid.NewGuid(),
            IsActive = true
        };

    private sealed class FakeStorage : IFileStorageService
    {
        public int UploadCount { get; private set; }
        public int DeleteCount { get; private set; }

        public Task<string> UploadAsync(
            Stream stream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            UploadCount++;
            return Task.FromResult("/uploads/receipts/receipt.jpg");
        }

        public Task DeleteAsync(string receiptUrl, CancellationToken cancellationToken)
        {
            DeleteCount++;
            return Task.CompletedTask;
        }

        public Task<StoredFile> OpenReadAsync(
            string receiptUrl,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeDbContext(ExpenseRequest expense, User actor)
        : IApplicationDbContext
    {
        public ExpenseRequest? UpdatedExpense { get; private set; }
        public int SaveCount { get; private set; }
        public Exception? SaveException { get; init; }

        public Task<ExpenseRequest?> GetExpenseRequestAsync(
            Guid expenseRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(expenseRequestId == expense.Id ? expense : null);

        public Task<User?> GetUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == actor.Id ? actor : null);

        public Task<User?> GetUserByEmailAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<Department?> GetDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Department?>(null);

        public Task<IReadOnlyList<ApprovalHistory>> GetApprovalHistoriesAsync(
            Guid expenseRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ApprovalHistory>>([]);

        public void AddExpenseRequest(ExpenseRequest expenseRequest) { }
        public void AddUser(User user) { }

        public void UpdateExpenseRequest(
            ExpenseRequest expenseRequest,
            byte[] originalRowVersion) =>
            UpdatedExpense = expenseRequest;

        public void AddApprovalHistory(ApprovalHistory approvalHistory) { }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return SaveException is null
                ? Task.FromResult(1)
                : Task.FromException<int>(SaveException);
        }
    }
}
