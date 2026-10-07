using ExpenseApproval.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ExpenseApproval.Infrastructure.Data;

namespace ExpenseApproval.Api.Development;

public static class DevelopmentDataSeeder
{
    private static readonly Guid DepartmentId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(
        AppDbContext dbContext,
        string password,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (!await dbContext.Departments.AnyAsync(
                department => department.Id == DepartmentId,
                cancellationToken))
        {
            dbContext.Departments.Add(new Department
            {
                Id = DepartmentId,
                Name = "IT Department",
                MonthlyBudget = 500_000_000,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var passwordHasher = new PasswordHasher<User>();
        var users = new[]
        {
            CreateUser(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Test Employee",
                "emp@local",
                "Employee"),
            CreateUser(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Test TeamLead",
                "lead@local",
                "TeamLead"),
            CreateUser(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "Test Manager",
                "mgr@local",
                "Manager"),
            CreateUser(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "Test Accountant",
                "acc@local",
                "Accountant")
        };

        foreach (var user in users)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
        }

        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(
        Guid id,
        string fullName,
        string email,
        string role) =>
        new()
        {
            Id = id,
            DepartmentId = DepartmentId,
            FullName = fullName,
            Email = email,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
