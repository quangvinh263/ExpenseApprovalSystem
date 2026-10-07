using ExpenseApproval.Domain.Entities;
using ExpenseApproval.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseApproval.Application.Tests;

public sealed class AppDbContextTests
{
    [Fact]
    public async Task Expense_request_can_be_saved_and_queried_without_tracking()
    {
        await using var context = CreateContext();
        var request = CreateRequest();

        context.AddExpenseRequest(request);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var loaded = await context.GetExpenseRequestAsync(request.Id);

        Assert.NotNull(loaded);
        Assert.Equal(request.Id, loaded.Id);
        Assert.DoesNotContain(
            context.ChangeTracker.Entries<ExpenseRequest>(),
            entry => entry.State == EntityState.Unchanged);
    }

    [Fact]
    public async Task Approval_history_is_append_only()
    {
        await using var context = CreateContext();
        var history = new ApprovalHistory
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = Guid.NewGuid(),
            ActionByUserId = Guid.NewGuid(),
            Action = "Submit",
            FromStatus = "Draft",
            ToStatus = "PendingApproval",
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.AddApprovalHistory(history);
        await context.SaveChangesAsync();

        context.Entry(history).State = EntityState.Modified;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Row_version_is_configured_as_required_concurrency_token()
    {
        using var context = CreateContext();
        var property = context.Model
            .FindEntityType(typeof(ExpenseRequest))!
            .FindProperty(nameof(ExpenseRequest.RowVersion))!;

        Assert.True(property.IsConcurrencyToken);
        Assert.False(property.IsNullable);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ExpenseRequest CreateRequest() =>
        new()
        {
            Id = Guid.NewGuid(),
            RequesterId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Amount = 100,
            Category = "Travel",
            Reason = "Business travel",
            Status = "Draft",
            RowVersion = [1],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
}
