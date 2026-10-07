using ExpenseApproval.Application.Abstractions;
using MediatR;

namespace ExpenseApproval.Application.Auth;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(
    string AccessToken,
    Guid UserId,
    string FullName,
    string Email,
    string Role);

public sealed class LoginUserCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginUserCommand, LoginResult>
{
    public async Task<LoginResult> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.GetUserByEmailAsync(email, cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !passwordService.Verify(user, user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return new LoginResult(
            jwtTokenService.CreateToken(user),
            user.Id,
            user.FullName,
            user.Email,
            user.Role);
    }
}
