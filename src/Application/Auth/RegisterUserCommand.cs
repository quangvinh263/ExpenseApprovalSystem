using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using MediatR;

namespace ExpenseApproval.Application.Auth;

public sealed record RegisterUserCommand(
    string FullName,
    string Email,
    string Password,
    Guid DepartmentId) : IRequest<RegisteredUser>;

public sealed record RegisteredUser(Guid Id, string FullName, string Email, string Role);

public sealed class RegisterUserCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordService passwordService)
    : IRequestHandler<RegisterUserCommand, RegisteredUser>
{
    public async Task<RegisteredUser> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(request.FullName));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("A valid email is required.", nameof(request.Email));
        }

        if (request.Password.Length < 8)
        {
            throw new ArgumentException(
                "Password must contain at least 8 characters.",
                nameof(request.Password));
        }

        var department = await dbContext.GetDepartmentAsync(
            request.DepartmentId,
            cancellationToken);
        if (department is null || !department.IsActive)
        {
            throw new UnauthorizedAccessException("The selected department is not active.");
        }

        if (await dbContext.GetUserByEmailAsync(email, cancellationToken) is not null)
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Role = "Employee",
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordService.Hash(user, request.Password);

        dbContext.AddUser(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisteredUser(user.Id, user.FullName, user.Email, user.Role);
    }
}
