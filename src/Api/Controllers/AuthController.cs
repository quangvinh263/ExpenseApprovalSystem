using ExpenseApproval.Api.Contracts;
using ExpenseApproval.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseApproval.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    public sealed record RegisterRequest(
        string FullName,
        string Email,
        string Password,
        Guid DepartmentId);

    public sealed record LoginRequest(string Email, string Password);

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegisteredUser>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await sender.Send(
                new RegisterUserCommand(
                    request.FullName,
                    request.Email,
                    request.Password,
                    request.DepartmentId),
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new ApiResponse<RegisteredUser>(user));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new LoginUserCommand(request.Email, request.Password),
                cancellationToken);

            return Ok(new ApiResponse<LoginResult>(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
