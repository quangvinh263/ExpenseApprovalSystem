using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Workflow;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ExpenseApproval.Application.Expenses;

public sealed record UploadReceiptCommand(
    Guid ExpenseRequestId,
    IFormFile File) : IRequest<string>;

public sealed class UploadReceiptCommandHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorageService)
    : IRequestHandler<UploadReceiptCommand, string>
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "application/pdf"
    ];

    public async Task<string> Handle(
        UploadReceiptCommand request,
        CancellationToken cancellationToken)
    {
        ValidateFile(request.File);

        var expenseRequest = await dbContext.GetExpenseRequestAsync(
            request.ExpenseRequestId,
            cancellationToken);

        if (expenseRequest is null)
        {
            throw new KeyNotFoundException(
                $"Expense request '{request.ExpenseRequestId}' was not found.");
        }

        if (expenseRequest.Status is not ExpenseWorkflow.Draft and not ExpenseWorkflow.Rejected)
        {
            throw new InvalidOperationException(
                "A receipt can only be uploaded for a draft or rejected expense request.");
        }

        await using var stream = request.File.OpenReadStream();
        var receiptUrl = await fileStorageService.UploadAsync(
            stream,
            request.File.FileName,
            request.File.ContentType,
            cancellationToken);

        var updatedExpenseRequest = expenseRequest with
        {
            ReceiptUrl = receiptUrl,
            UpdatedAt = DateTimeOffset.UtcNow,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        dbContext.UpdateExpenseRequest(updatedExpenseRequest, expenseRequest.RowVersion);
        await dbContext.SaveChangesAsync(cancellationToken);

        return receiptUrl;
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("Receipt file is required.", nameof(file));
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException(
                "Receipt file must not exceed 5 MB.",
                nameof(file));
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new ArgumentException(
                "Receipt file must be a JPEG, PNG, or PDF.",
                nameof(file));
        }
    }
}
