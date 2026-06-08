using Microblog.Api.Infrastructure.Storage;

namespace Microblog.Api.Services;

public class MediaService(
    AppDbContext dbContext,
    IConnectionMultiplexer connectionMultiplexer,
    IStorageService storageService)
    : IMediaService
{
    private readonly IDatabase _inMemoryDb = connectionMultiplexer.GetDatabase();

    public async Task SaveMediaFileAsync(IFormFile file, long entityId, AppConstants.MediaEntityType entityType)
    {
        if (file == null || file.Length == 0) throw new Exception("No file provided.");

        string relativePath = GenerateRelativePath(entityId, entityType);

        string storedPath = await storageService.SaveFileAsync(file, relativePath);

        var media = new MediaFile
        {
            EntityId = entityId,
            EntityType = entityType,
            FileName = Path.GetFileName(storedPath),
            FilePath = storedPath,
            MimeType = file.ContentType,
            FileSize = file.Length
        };

        MediaFileUtils.IsMediaFileValid(media);

        await dbContext.MediaFiles.AddAsync(media);
        await dbContext.SaveChangesAsync();

        try
        {
            await _inMemoryDb.StringSetAsync(
                InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH, entityType, entityId),
                storedPath);
        }
        catch { /* cache failure is non-critical */ }
    }

    public async Task<string> GetMediaFilePathAsync(long entityId, AppConstants.MediaEntityType entityType)
    {
        if (entityId <= 0) throw new Exception("Invalid entity id");

        try
        {
            string key = InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH, entityType, entityId);
            string? filePath = Convert.ToString(await _inMemoryDb.StringGetAsync(key));
            if (!string.IsNullOrEmpty(filePath)) return storageService.GetUrl(filePath);
        }
        catch { /* fall through to DB */ }

        return await GetMediaFilePathFallbackDbAsync(entityId, entityType);
    }

    public async Task<string> GetMediaFilePathFallbackDbAsync(long entityId, AppConstants.MediaEntityType entityType,
        bool isFallback = true)
    {
        if (entityId <= 0) throw new Exception("Invalid entity id");

        var mediaFile =
            await dbContext.MediaFiles.FirstOrDefaultAsync(m => m.EntityType == entityType && m.EntityId == entityId);
        if (mediaFile == null) throw new Exception("File doesn't exist");

        if (isFallback)
        {
            try
            {
                await _inMemoryDb.StringSetAsync(
                    InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH, entityType, entityId),
                    mediaFile.FilePath);
            }
            catch { }
        }

        return storageService.GetUrl(mediaFile.FilePath);
    }

    public async Task DeleteMediaFileAsync(long entityId, AppConstants.MediaEntityType entityType)
    {
        if (entityId <= 0) throw new Exception("Invalid entity id");
        var mediaFile =
            await dbContext.MediaFiles.FirstOrDefaultAsync(m => m.EntityType == entityType && m.EntityId == entityId);
        if (mediaFile == null) throw new Exception("File doesn't exist");

        await storageService.DeleteFileAsync(mediaFile.FilePath);
        dbContext.MediaFiles.Remove(mediaFile);
        await dbContext.SaveChangesAsync();

        try
        {
            await _inMemoryDb.KeyDeleteAsync(
                InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH, entityType, entityId));
        }
        catch { }
    }

    private static string GenerateRelativePath(long entityId, AppConstants.MediaEntityType entityType)
    {
        if (entityId <= 0) throw new Exception("Invalid entity id");

        return entityType switch
        {
            AppConstants.MediaEntityType.User => $"/uploads/users/{entityId}/profile_{Guid.NewGuid()}.jpg",
            AppConstants.MediaEntityType.Post => $"/uploads/posts/{entityId}/{Guid.NewGuid()}.jpg",
            _ => throw new Exception("Invalid entity type")
        };
    }
}
