using Microsoft.EntityFrameworkCore;
using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Application.Expenses;
using ExpenseApproval.Infrastructure.Data;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(serviceProvider =>
    serviceProvider.GetRequiredService<AppDbContext>());
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssembly(typeof(CreateExpenseCommand).Assembly));
builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExpenseApproval.Infrastructure.Data.AppDbContext>();
    db.Database.EnsureCreated();

    var deptId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var employeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    var teamLeadId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    var managerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    var accountantId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    if (!db.Departments.Any(d => d.Id == deptId))
    {
        db.Departments.Add(new ExpenseApproval.Domain.Entities.Department
        {
            Id = deptId, Name = "IT Department", MonthlyBudget = 500000000, IsActive = true, CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();
    }

    if (!db.Users.Any(u => u.Id == employeeId))
    {
        db.Users.AddRange(
            new ExpenseApproval.Domain.Entities.User { Id = employeeId, DepartmentId = deptId, FullName = "Test Employee", Email = "emp@local", PasswordHash = "123", Role = "Employee", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new ExpenseApproval.Domain.Entities.User { Id = teamLeadId, DepartmentId = deptId, FullName = "Test TeamLead", Email = "lead@local", PasswordHash = "123", Role = "TeamLead", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new ExpenseApproval.Domain.Entities.User { Id = managerId, DepartmentId = deptId, FullName = "Test Manager", Email = "mgr@local", PasswordHash = "123", Role = "Manager", IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
        );
        db.SaveChanges();
    }

    // Chèn thêm Accountant cho luồng Mark Paid
    if (!db.Users.Any(u => u.Id == accountantId))
    {
        db.Users.Add(new ExpenseApproval.Domain.Entities.User
        {
            Id = accountantId, DepartmentId = deptId, FullName = "Test Accountant", Email = "acc@local", PasswordHash = "123", Role = "Accountant", IsActive = true, CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();
    }
}



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



app.UseHttpsRedirection();

app.MapControllers();
app.Run();
