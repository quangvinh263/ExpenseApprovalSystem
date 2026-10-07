using ExpenseApproval.Api.Contracts;
using ExpenseApproval.Api.Auth;
using ExpenseApproval.Application.Expenses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseApproval.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public sealed class ExpensesController(ISender sender) : ControllerBase
{
    public sealed record CreateExpenseRequest(
        Guid DepartmentId,
        decimal Amount,
        string Category,
        string Reason);
    public sealed record SubmitExpenseRequest(byte[] RowVersion);
    public sealed record ExpenseActionRequest(string? Comment, byte[] RowVersion);

    [HttpPost]
    [Authorize(Policy = "ExpenseCreator")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateExpenseCommand(
            User.GetRequiredUserId(),
            request.DepartmentId,
            request.Amount,
            request.Category,
            request.Reason);
        try
        {
            var expenseRequestId = await sender.Send(command, cancellationToken);

            return Created(
                $"/api/expenses/{expenseRequestId}",
                new ApiResponse<Guid>(expenseRequestId));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = "ExpenseCreator")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(
        Guid id,
        [FromBody] SubmitExpenseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var expenseRequestId = await sender.Send(
                new SubmitExpenseCommand(id, User.GetRequiredUserId(), request.RowVersion),
                cancellationToken);

            return Ok(new ApiResponse<Guid>(expenseRequestId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "ExpenseApprover")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ExpenseActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var expenseRequestId = await sender.Send(
                new ApproveExpenseCommand(id, User.GetRequiredUserId(), request.Comment, request.RowVersion),
                cancellationToken);

            return Ok(new ApiResponse<Guid>(expenseRequestId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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

    [HttpPost("{id:guid}/pay")]
    [Authorize(Policy = "ExpensePayer")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkPaid(
        Guid id,
        [FromBody] SubmitExpenseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var expenseRequestId = await sender.Send(
                new MarkExpensePaidCommand(id, User.GetRequiredUserId(), request.RowVersion),
                cancellationToken);

            return Ok(new ApiResponse<Guid>(expenseRequestId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "ExpenseApprover")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] ExpenseActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var expenseRequestId = await sender.Send(
                new RejectExpenseCommand(id, User.GetRequiredUserId(), request.Comment, request.RowVersion),
                cancellationToken);

            return Ok(new ApiResponse<Guid>(expenseRequestId));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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
}
