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
        await stream.CopyToAsync(output, cancellationToken);

        return $"/uploads/receipts/{storedFileName}";
    }
}
