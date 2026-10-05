using ExpenseApproval.Api.Contracts;
using ExpenseApproval.Application.Expenses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseApproval.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public sealed class ExpensesController(ISender sender) : ControllerBase
{
    public sealed record SubmitExpenseRequest(Guid UserId);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var expenseRequestId = await sender.Send(command, cancellationToken);

        return Created(
            $"/api/expenses/{expenseRequestId}",
            new ApiResponse<Guid>(expenseRequestId));
    }

    [HttpPost("{id:guid}/submit")]
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
                new SubmitExpenseCommand(id, request.UserId),
                cancellationToken);

            return Ok(new ApiResponse<Guid>(expenseRequestId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }
}
