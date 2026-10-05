namespace ExpenseApproval.Api.Contracts;

public sealed record ApiResponse<T>(T Data);
