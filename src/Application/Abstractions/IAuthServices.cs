using ExpenseApproval.Domain.Entities;

namespace ExpenseApproval.Application.Abstractions;

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string hashedPassword, string providedPassword);
}

public interface IJwtTokenService
{
    string CreateToken(User user);
}
