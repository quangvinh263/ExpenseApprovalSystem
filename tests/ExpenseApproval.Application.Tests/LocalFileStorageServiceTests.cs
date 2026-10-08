using ExpenseApproval.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace ExpenseApproval.Application.Tests;

public sealed class LocalFileStorageServiceTests
{
    [Fact]
    public async Task Upload_uses_guid_name_and_receipt_directory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"expense-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var service = new LocalFileStorageService(new TestEnvironment(root));
            await using var input = new MemoryStream([0xFF, 0xD8, 0xFF]);

            var url = await service.UploadAsync(
                input,
                "../../unsafe.jpg",
                "image/jpeg",
                CancellationToken.None);

            Assert.StartsWith("/uploads/receipts/", url);
            var fileName = url["/uploads/receipts/".Length..];
            Assert.Matches("^[a-f0-9]{32}\\.jpg$", fileName);
            Assert.True(File.Exists(Path.Combine(root, "uploads", "receipts", fileName)));

            await service.DeleteAsync(url, CancellationToken.None);
            Assert.False(File.Exists(Path.Combine(root, "uploads", "receipts", fileName)));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task OpenRead_rejects_path_traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), $"expense-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var service = new LocalFileStorageService(new TestEnvironment(root));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.OpenReadAsync(
                    "/uploads/receipts/../secret.pdf",
                    CancellationToken.None));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public string WebRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
