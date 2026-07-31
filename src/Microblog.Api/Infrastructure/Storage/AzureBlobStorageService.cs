using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Microblog.Api.Infrastructure.Storage;

public sealed class AzureBlobStorageService(IConfiguration config, ILogger<AzureBlobStorageService> logger)
    : IStorageService
{
    private readonly BlobContainerClient _container = new(
        config["Azure:BlobConnectionString"]
            ?? throw new InvalidOperationException("Azure:BlobConnectionString is required for blob storage"),
        config["Azure:BlobContainerName"] ?? "microblog-media");

    public async Task<string> SaveFileAsync(IFormFile file, string relativePath, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        string blobName = relativePath.TrimStart('/');
        BlobClient blob = _container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } }, ct);

        logger.LogInformation("Uploaded blob {BlobName}", blobName);
        return blobName;
    }

    public async Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        string blobName = path.TrimStart('/');
        await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
    }

    public string GetUrl(string path)
    {
        string blobName = path.TrimStart('/');
        BlobClient blob = _container.GetBlobClient(blobName);

        if (blob.CanGenerateSasUri)
            return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)).ToString();

        return blob.Uri.ToString();
    }
}
