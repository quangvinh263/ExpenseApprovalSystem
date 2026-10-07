using System.Security.Claims;

namespace ExpenseApproval.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("The authenticated user identifier is invalid.");
        }

        return userId;
    }
}
