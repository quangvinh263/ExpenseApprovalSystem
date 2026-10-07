using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ExpenseApproval.Api.Auth;

public sealed class PasswordService(IPasswordHasher<User> passwordHasher)
    : IPasswordService
{
    public string Hash(User user, string password) =>
        passwordHasher.HashPassword(user, password);

    public bool Verify(User user, string hashedPassword, string providedPassword) =>
        passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword)
        is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
}
