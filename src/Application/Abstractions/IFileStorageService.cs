namespace ExpenseApproval.Application.Abstractions;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string receiptUrl,
        CancellationToken cancellationToken);

    Task<StoredFile> OpenReadAsync(
        string receiptUrl,
        CancellationToken cancellationToken);
}

public sealed record StoredFile(Stream Content, string ContentType, string FileName);
