using System.Runtime.ExceptionServices;
using ExpenseApproval.Application.Abstractions;
using ExpenseApproval.Domain.Entities;
using ExpenseApproval.Domain.Workflow;
using MediatR;

namespace ExpenseApproval.Application.Expenses;

public sealed record UploadReceiptCommand(
    Guid ExpenseRequestId,
    Guid ActorId,
    Stream File,
    string FileName,
    string ContentType,
    long Length) : IRequest<string>;

public sealed class UploadReceiptCommandHandler(
    IApplicationDbContext dbContext,
    IFileStorageService fileStorageService)
    : IRequestHandler<UploadReceiptCommand, string>
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "image/jpeg",
            "image/png",
            "application/pdf"
        };

    public async Task<string> Handle(
        UploadReceiptCommand request,
        CancellationToken cancellationToken)
    {
        ValidateFile(request);

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

        if (expenseRequest.Status is not ExpenseWorkflow.Draft and not ExpenseWorkflow.Rejected)
        {
            throw new InvalidOperationException(
                "A receipt can only be uploaded for a draft or rejected expense request.");
        }

        var receiptUrl = await fileStorageService.UploadAsync(
            request.File,
            request.FileName,
            request.ContentType,
            cancellationToken);

        try
        {
            var updatedExpenseRequest = expenseRequest with
            {
                ReceiptUrl = receiptUrl,
                UpdatedAt = DateTimeOffset.UtcNow,
                RowVersion = Guid.NewGuid().ToByteArray()
            };

            dbContext.UpdateExpenseRequest(updatedExpenseRequest, expenseRequest.RowVersion);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception persistenceException)
        {
            try
            {
                await fileStorageService.DeleteAsync(receiptUrl, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                throw new InvalidOperationException(
                    "Receipt persistence failed and the uploaded file could not be cleaned up.",
                    new AggregateException(persistenceException, cleanupException));
            }

            ExceptionDispatchInfo.Capture(persistenceException).Throw();
            throw;
        }

        return receiptUrl;
    }

    private static void ValidateFile(UploadReceiptCommand request)
    {
        if (request.File is null || request.Length <= 0)
        {
            throw new ArgumentException("Receipt file is required.", nameof(request.File));
        }

        if (request.Length > MaxFileSize)
        {
            throw new ArgumentException(
                "Receipt file must not exceed 5 MB.",
                nameof(request.File));
        }

        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            throw new ArgumentException(
                "Receipt file must be a JPEG, PNG, or PDF.",
                nameof(request.ContentType));
        }

        if (!request.File.CanSeek)
        {
            throw new ArgumentException(
                "Receipt stream must support validation.",
                nameof(request.File));
        }

        var originalPosition = request.File.Position;
        try
        {
            Span<byte> header = stackalloc byte[8];
            var bytesRead = request.File.Read(header);
            if (!HasValidSignature(request.ContentType, header[..bytesRead]))
            {
                throw new ArgumentException(
                    "Receipt content does not match its declared content type.",
                    nameof(request.ContentType));
            }
        }
        finally
        {
            request.File.Position = originalPosition;
        }
    }

    private static bool HasValidSignature(string contentType, ReadOnlySpan<byte> header) =>
        contentType switch
        {
            "image/jpeg" => header.Length >= 3 &&
                header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => header.SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "application/pdf" => header.Length >= 5 &&
                header[..5].SequenceEqual("%PDF-"u8),
            _ => false
        };
}

internal static class ExpenseReceiptAuthorization
{
    public static void EnsureCanUpload(ExpenseRequest expenseRequest, User? actor)
    {
        if (actor is null || !actor.IsActive)
        {
            throw new UnauthorizedAccessException(
                "The user is not authorized to upload this receipt.");
        }

        var elevatedSameDepartment =
            actor.DepartmentId == expenseRequest.DepartmentId &&
            actor.Role is ExpenseWorkflow.TeamLead
                or ExpenseWorkflow.Manager
                or ExpenseWorkflow.Accountant;

        if (actor.Id != expenseRequest.RequesterId &&
            actor.Role != ExpenseWorkflow.Admin &&
            !elevatedSameDepartment)
        {
            throw new UnauthorizedAccessException(
                "The user is not authorized to upload this receipt.");
        }
    }
}
