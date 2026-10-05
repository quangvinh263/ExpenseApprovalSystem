using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseApproval.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ExpenseRequest> ExpenseRequests => Set<ExpenseRequest>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();

    public async Task<ExpenseRequest?> GetExpenseRequestAsync(
        Guid expenseRequestId,
        CancellationToken cancellationToken = default) =>
        await ExpenseRequests.AsNoTracking().SingleOrDefaultAsync(
            expenseRequest => expenseRequest.Id == expenseRequestId,
            cancellationToken);

    public void AddExpenseRequest(ExpenseRequest expenseRequest) => ExpenseRequests.Add(expenseRequest);
    public void UpdateExpenseRequest(ExpenseRequest expenseRequest) => ExpenseRequests.Update(expenseRequest);
    public void AddApprovalHistory(ApprovalHistory approvalHistory) => ApprovalHistories.Add(approvalHistory);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(department => department.Id);
            entity.Property(department => department.Id).HasColumnType("uuid");
            entity.Property(department => department.Name).HasMaxLength(100).IsRequired();
            entity.Property(department => department.MonthlyBudget).HasPrecision(18, 2).IsRequired();
            entity.Property(department => department.IsActive).HasDefaultValue(true).IsRequired();
            entity.Property(department => department.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnType("uuid");
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).HasColumnType("text").IsRequired();
            entity.Property(user => user.Role).HasMaxLength(30).IsRequired();
            entity.Property(user => user.DepartmentId).HasColumnType("uuid");
            entity.Property(user => user.IsActive).HasDefaultValue(true).IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            entity.HasOne(user => user.Department)
                .WithMany(department => department.Users)
                .HasForeignKey(user => user.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExpenseRequest>(entity =>
        {
            entity.ToTable("ExpenseRequests");
            entity.HasKey(request => request.Id);
            entity.Property(request => request.Id).HasColumnType("uuid");
            entity.Property(request => request.RequesterId).HasColumnType("uuid").IsRequired();
            entity.Property(request => request.DepartmentId).HasColumnType("uuid").IsRequired();
            entity.Property(request => request.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(request => request.Category).HasMaxLength(50).IsRequired();
            entity.Property(request => request.Reason).HasColumnType("text").IsRequired();
            entity.Property(request => request.ReceiptUrl).HasColumnType("text");
            entity.Property(request => request.Status).HasMaxLength(30).IsRequired();
            entity.Property(request => request.CurrentApproverRole).HasMaxLength(30);
            entity.Property(request => request.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(request => request.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(request => request.RowVersion)
                .HasColumnType("bytea")
                .IsRequired()
                .IsConcurrencyToken();

            entity.HasOne(request => request.Requester)
                .WithMany(user => user.ExpenseRequests)
                .HasForeignKey(request => request.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.Department)
                .WithMany(department => department.ExpenseRequests)
                .HasForeignKey(request => request.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            entity.ToTable("ApprovalHistories");
            entity.HasKey(history => history.Id);
            entity.Property(history => history.Id).HasColumnType("uuid");
            entity.Property(history => history.ExpenseRequestId).HasColumnType("uuid").IsRequired();
            entity.Property(history => history.ActionByUserId).HasColumnType("uuid").IsRequired();
            entity.Property(history => history.Action).HasMaxLength(30).IsRequired();
            entity.Property(history => history.FromStatus).HasMaxLength(30).IsRequired();
            entity.Property(history => history.ToStatus).HasMaxLength(30).IsRequired();
            entity.Property(history => history.Comment).HasColumnType("text");
            entity.Property(history => history.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            entity.HasOne(history => history.ExpenseRequest)
                .WithMany(request => request.ApprovalHistories)
                .HasForeignKey(history => history.ExpenseRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(history => history.ActionByUser)
                .WithMany(user => user.ApprovalHistories)
                .HasForeignKey(history => history.ActionByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
