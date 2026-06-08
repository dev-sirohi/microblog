namespace Microblog.Api.Infrastructure.Storage;

/// <summary>Saves files to the local wwwroot/uploads directory (default, no Azure required).</summary>
public sealed class LocalStorageService(IWebHostEnvironment env) : IStorageService
{
    public async Task<string> SaveFileAsync(IFormFile file, string relativePath, CancellationToken ct = default)
    {
        string fullPath = Path.Combine(env.ContentRootPath, "wwwroot", relativePath.TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        return relativePath;
    }

    public Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        string fullPath = Path.Combine(env.ContentRootPath, "wwwroot", path.TrimStart('/'));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetUrl(string path) => $"{AppConstants.BASE_URL}{path}";
}
