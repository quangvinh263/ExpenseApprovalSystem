using ExpenseApproval.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace ExpenseApproval.Infrastructure.Storage;

public sealed class LocalFileStorageService(IWebHostEnvironment environment)
    : IFileStorageService
{
    private static readonly IReadOnlyDictionary<string, string> AllowedExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["application/pdf"] = ".pdf"
        };

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!AllowedExtensions.TryGetValue(contentType, out var extension))
        {
            throw new ArgumentException("Unsupported receipt content type.", nameof(contentType));
        }

        var directory = Path.Combine(
            environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
            "uploads",
            "receipts");
        Directory.CreateDirectory(directory);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(directory, storedFileName);

        await using var output = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        try
        {
            await stream.CopyToAsync(output, cancellationToken);
        }
        catch
        {
            output.Close();
            File.Delete(path);
            throw;
        }

        return $"/uploads/receipts/{storedFileName}";
    }

    public Task DeleteAsync(
        string receiptUrl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(receiptUrl);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<StoredFile> OpenReadAsync(
        string receiptUrl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(receiptUrl);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Receipt file was not found.", path);
        }

        var extension = Path.GetExtension(path);
        var contentType = AllowedExtensions
            .FirstOrDefault(pair =>
                pair.Value.Equals(extension, StringComparison.OrdinalIgnoreCase))
            .Key;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Unsupported receipt file.", nameof(receiptUrl));
        }
        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult(
            new StoredFile(stream, contentType, Path.GetFileName(path)));
    }

    private string ResolvePath(string receiptUrl)
    {
        const string prefix = "/uploads/receipts/";
        if (!receiptUrl.StartsWith(prefix, StringComparison.Ordinal) ||
            receiptUrl.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid receipt URL.", nameof(receiptUrl));
        }

        var fileName = Path.GetFileName(receiptUrl[prefix.Length..]);
        if (string.IsNullOrWhiteSpace(fileName) ||
            !fileName.Equals(receiptUrl[prefix.Length..], StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid receipt URL.", nameof(receiptUrl));
        }

        var directory = Path.GetFullPath(Path.Combine(
            environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
            "uploads",
            "receipts"));
        var path = Path.GetFullPath(Path.Combine(directory, fileName));
        if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid receipt URL.", nameof(receiptUrl));
        }

        return path;
    }
}
