namespace Microblog.Api.Interfaces.ServiceInterfaces;

public interface IMediaService
{
    Task SaveMediaFileAsync(IFormFile file, long entityId, AppConstants.MediaEntityType entityType);
    Task<string> GetMediaFilePathAsync(long entityId, AppConstants.MediaEntityType entityType);

    Task<string> GetMediaFilePathFallbackDbAsync(long entityId, AppConstants.MediaEntityType entityType,
        bool isFallback = true);
}