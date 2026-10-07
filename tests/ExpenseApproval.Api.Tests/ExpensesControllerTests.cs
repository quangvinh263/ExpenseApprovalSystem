using System.Security.Claims;
using ExpenseApproval.Api.Contracts;
using ExpenseApproval.Api.Controllers;
using ExpenseApproval.Application.Expenses;
using ExpenseApproval.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ExpenseApproval.Api.Tests;

public sealed class ExpensesControllerTests
{
    [Fact]
    public async Task Create_returns_created_and_uses_user_id_from_claims()
    {
        var sender = new Mock<ISender>();
        var departmentId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        sender
            .Setup(mock => mock.Send(
                It.IsAny<CreateExpenseCommand>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((request, _) =>
            {
                var command = Assert.IsType<CreateExpenseCommand>(request);
                Assert.Equal(requesterId, command.RequesterId);
                Assert.Equal(departmentId, command.DepartmentId);
            })
            .ReturnsAsync(expenseId);

        var controller = CreateController(sender, requesterId);
        var result = await controller.Create(
            new ExpensesController.CreateExpenseRequest(
                departmentId,
                125,
                "Travel",
                "Business travel"),
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal($"/api/expenses/{expenseId}", created.Location);
    }

    [Fact]
    public async Task Reject_returns_bad_request_when_comment_is_missing()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(mock => mock.Send(
                It.IsAny<RejectExpenseCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("A rejection comment is required."));

        var controller = CreateController(sender, Guid.NewGuid());
        var result = await controller.Reject(
            Guid.NewGuid(),
            new ExpensesController.ExpenseActionRequest(null, [1]),
            CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            Assert.IsType<BadRequestObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Approve_returns_forbidden_when_handler_rejects_actor()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(mock => mock.Send(
                It.IsAny<ApproveExpenseCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        var controller = CreateController(sender, Guid.NewGuid());
        var result = await controller.Approve(
            Guid.NewGuid(),
            new ExpensesController.ExpenseActionRequest("Not allowed", [1]),
            CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    [Fact]
    public async Task Submit_returns_conflict_for_invalid_state_or_concurrency()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(mock => mock.Send(
                It.IsAny<SubmitExpenseCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        var controller = CreateController(sender, Guid.NewGuid());
        var result = await controller.Submit(
            Guid.NewGuid(),
            new ExpensesController.SubmitExpenseRequest([1]),
            CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            Assert.IsType<ConflictResult>(result).StatusCode);
    }

    [Fact]
    public async Task Get_returns_not_found_when_expense_does_not_exist()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(mock => mock.Send(
                It.IsAny<GetExpenseQuery>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var controller = CreateController(sender, Guid.NewGuid());
        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            Assert.IsType<NotFoundResult>(result).StatusCode);
    }

    [Fact]
    public async Task Get_approval_history_returns_ok_with_history_entries()
    {
        var sender = new Mock<ISender>();
        var expenseId = Guid.NewGuid();
        var historyId = Guid.NewGuid();
        sender
            .Setup(mock => mock.Send(
                It.IsAny<GetApprovalHistoryQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ApprovalHistory
                {
                    Id = historyId,
                    ExpenseRequestId = expenseId,
                    ActionByUserId = Guid.NewGuid(),
                    Action = "Submit",
                    FromStatus = "Draft",
                    ToStatus = "PendingApproval",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]);

        var controller = CreateController(sender, Guid.NewGuid());
        var result = await controller.GetApprovalHistory(
            expenseId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<ExpensesController.ApprovalHistoryResponse>>>(
            ok.Value);
        var history = Assert.Single(response.Data);
        Assert.Equal(historyId, history.Id);
        Assert.Equal("Submit", history.Action);
    }

    [Fact]
    public async Task Get_approval_history_returns_not_found_when_expense_does_not_exist()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(mock => mock.Send(
                It.IsAny<GetApprovalHistoryQuery>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException());

        var controller = CreateController(sender, Guid.NewGuid());
        var result = await controller.GetApprovalHistory(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            Assert.IsType<NotFoundResult>(result).StatusCode);
    }

    private static ExpensesController CreateController(
        Mock<ISender> sender,
        Guid userId)
    {
        var controller = new ExpensesController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ], "Test"))
                }
            }
        };

        return controller;
    }
}
