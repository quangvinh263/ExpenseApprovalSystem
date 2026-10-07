using ExpenseApproval.Domain.Entities;
using ExpenseApproval.Domain.Workflow;
using Xunit;

namespace ExpenseApproval.Application.Tests;

public sealed class ExpenseWorkflowTests
{
    [Fact]
    public void Submit_routes_team_lead_at_or_below_limit()
    {
        var request = CreateRequest(10_000_000m, ExpenseWorkflow.Draft);

        var result = ExpenseWorkflow.Submit(
            request,
            ExpenseWorkflow.TeamLead,
            DateTimeOffset.UtcNow,
            [2]);

        Assert.Equal(ExpenseWorkflow.PendingApproval, result.Status);
        Assert.Equal(ExpenseWorkflow.TeamLead, result.CurrentApproverRole);
    }

    [Fact]
    public void Submit_routes_manager_above_limit()
    {
        var request = CreateRequest(10_000_000.01m, ExpenseWorkflow.Draft);

        var result = ExpenseWorkflow.Submit(
            request,
            ExpenseWorkflow.Manager,
            DateTimeOffset.UtcNow,
            [2]);

        Assert.Equal(ExpenseWorkflow.Manager, result.CurrentApproverRole);
    }

    [Fact]
    public void Approve_requires_pending_approval()
    {
        var request = CreateRequest(1m, ExpenseWorkflow.Draft);

        Assert.Throws<InvalidOperationException>(() =>
            ExpenseWorkflow.Approve(request, DateTimeOffset.UtcNow, [2]));
    }

    [Fact]
    public void Paid_request_cannot_transition_again()
    {
        var request = CreateRequest(1m, ExpenseWorkflow.Paid);

        Assert.Throws<InvalidOperationException>(() =>
            ExpenseWorkflow.MarkPaid(request, DateTimeOffset.UtcNow, [2]));
    }

    [Fact]
    public void Rejected_request_cannot_be_approved()
    {
        var request = CreateRequest(1m, ExpenseWorkflow.Rejected);

        Assert.Throws<InvalidOperationException>(() =>
            ExpenseWorkflow.Approve(request, DateTimeOffset.UtcNow, [2]));
    }

    private static ExpenseRequest CreateRequest(decimal amount, string status) =>
        new()
        {
            Id = Guid.NewGuid(),
            RequesterId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Amount = amount,
            Category = "Travel",
            Reason = "Business travel",
            Status = status,
            RowVersion = [1]
        };
}
