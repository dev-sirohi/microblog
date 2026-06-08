namespace Microblog.Api.Infrastructure.Storage;

/// <summary>
/// Abstracts file storage so <see cref="Services.MediaService"/> is independent of the backing store
/// (local filesystem or Azure Blob Storage).
/// </summary>
public interface IStorageService
{
    /// <summary>Saves a file and returns its public-accessible URL or relative path.</summary>
    Task<string> SaveFileAsync(IFormFile file, string relativePath, CancellationToken ct = default);

    /// <summary>Deletes a file by its path/blob name.</summary>
    Task DeleteFileAsync(string path, CancellationToken ct = default);

    /// <summary>Returns the public URL for a stored file path.</summary>
    string GetUrl(string path);
}
