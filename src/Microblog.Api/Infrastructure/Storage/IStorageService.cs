namespace Microblog.Api.Infrastructure.Storage;

public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string relativePath, CancellationToken ct = default);

    Task DeleteFileAsync(string path, CancellationToken ct = default);

    string GetUrl(string path);
}
