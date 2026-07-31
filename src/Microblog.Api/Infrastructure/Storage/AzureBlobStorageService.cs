using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Microblog.Api.Infrastructure.Storage;

/// <summary>
/// Stores media files in Azure Blob Storage and returns SAS URLs for secure access.
/// Uses Azurite locally via <c>Azure:BlobConnectionString = "UseDevelopmentStorage=true"</c>.
/// </summary>
public sealed class AzureBlobStorageService : IStorageService
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration config, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        string connectionString = config["Azure:BlobConnectionString"]
            ?? throw new InvalidOperationException("Azure:BlobConnectionString is required for blob storage");
        string containerName = config["Azure:BlobContainerName"] ?? "microblog-media";
        _container = new BlobContainerClient(connectionString, containerName);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> SaveFileAsync(IFormFile file, string relativePath, CancellationToken ct = default)
    {
        string blobName = relativePath.TrimStart('/');
        BlobClient blob = _container.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } }, ct);

        _logger.LogInformation("Uploaded blob {BlobName}", blobName);
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

        // Generate a SAS URL valid for 1 hour
        if (blob.CanGenerateSasUri)
        {
            var sasUri = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            return sasUri.ToString();
        }

        return blob.Uri.ToString();
    }
}
