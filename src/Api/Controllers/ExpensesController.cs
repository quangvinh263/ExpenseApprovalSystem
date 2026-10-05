using ExpenseApproval.Api.Contracts;
using ExpenseApproval.Application.Expenses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseApproval.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public sealed class ExpensesController(ISender sender) : ControllerBase
{
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
}
