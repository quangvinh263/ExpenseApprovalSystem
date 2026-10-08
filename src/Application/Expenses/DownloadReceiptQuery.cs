using ExpenseApproval.Application.Abstractions;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record DownloadReceiptQuery(
    Guid ExpenseRequestId,
    Guid ActorId) : IRequest<StoredFile>;

public sealed class DownloadReceiptQueryHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorageService)
    : IRequestHandler<DownloadReceiptQuery, StoredFile>
{
    public async Task<StoredFile> Handle(
        DownloadReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var expenseRequest = await dbContext.GetExpenseRequestAsync(
            request.ExpenseRequestId,
            cancellationToken);

        if (expenseRequest is null)
        {
            throw new KeyNotFoundException(
                $"Expense request '{request.ExpenseRequestId}' was not found.");
        }

        var actor = await dbContext.GetUserAsync(request.ActorId, cancellationToken);
        ExpenseReceiptAuthorization.EnsureCanUpload(expenseRequest, actor);

        if (string.IsNullOrWhiteSpace(expenseRequest.ReceiptUrl))
        {
            throw new KeyNotFoundException(
                $"Expense request '{request.ExpenseRequestId}' has no receipt.");
        }

        try
        {
            return await fileStorageService.OpenReadAsync(
                expenseRequest.ReceiptUrl,
                cancellationToken);
        }
        catch (FileNotFoundException exception)
        {
            throw new KeyNotFoundException(
                $"Receipt for expense request '{request.ExpenseRequestId}' was not found.",
                exception);
        }
    }
}
