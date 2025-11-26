namespace Microblog.Api.Services
{
    public class MediaService : IMediaService
    {
        private readonly AppDbContext _dbContext;
        private readonly IDatabase _inMemoryDb;
        private readonly IWebHostEnvironment _env;

        public MediaService(AppDbContext dbContext, IConnectionMultiplexer connectionMultiplexer, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _inMemoryDb = connectionMultiplexer.GetDatabase();
            _env = env;
        }

        public async Task SaveMediaFileAsync(IFormFile file, long entityId, AppConstants.MediaEntityType entityType)
        {
            if (file == null || file.Length == 0)
            {
                throw new Exception("No file provided.");
            }

            string relativePath = GenerateFilePath(entityId, entityType);
            string fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", relativePath.TrimStart('/'));
            string directory = Path.GetDirectoryName(fullPath)!;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory); 
            }

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            MediaFile media = new MediaFile
            {
                EntityId = entityId,
                EntityType = entityType,
                FileName = Path.GetFileName(fullPath),
                FilePath = relativePath,
                MimeType = file.ContentType,
                FileSize = file.Length
            };

            MediaFileUtils.IsMediaFileValid(media);

            await _dbContext.MediaFiles.AddAsync(media);
            await _dbContext.SaveChangesAsync();

            try
            {
                await _inMemoryDb.StringSetAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH, entityType, entityId), relativePath);
            }
            catch (Exception ex) { }
        }

        public async Task<string> GetMediaFilePathAsync(long entityId, AppConstants.MediaEntityType entityType)
        {
            if (entityId <= 0)
            {
                throw new Exception("Invalid entity id");
            }

            string filePath = string.Empty;

            try
            {
                string key_addMediaFilePath = InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH, entityType, entityId);
                filePath = Convert.ToString(await _inMemoryDb.StringGetAsync(key_addMediaFilePath))
                    ?? throw new Exception();
            }
            catch (Exception ex)
            {
                filePath = await GetMediaFilePathFallbackDbAsync(entityId, entityType);
            }

            return filePath;
        }

        public async Task<string> GetMediaFilePathFallbackDbAsync(long entityId, AppConstants.MediaEntityType entityType, bool isFallback = true)
        {
            if (entityId <= 0)
            {
                throw new Exception("Invalid entity id");
            }

            string filePath = string.Empty;

            MediaFile? mediaFile = await _dbContext.MediaFiles.FirstOrDefaultAsync(m => m.EntityType == entityType && m.EntityId == entityId);
            if (mediaFile == null)
            {
                throw new Exception("File doesn't exist");
            }

            if (isFallback)
            {
                try
                {
                    await _inMemoryDb.StringSetAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH, entityType, entityId), mediaFile.FilePath);
                } catch (Exception ex) { }
            }

            return mediaFile.FilePath;
        }

        private string GenerateFilePath(long entityId, AppConstants.MediaEntityType entityType)
        {
            if (entityId <= 0)
            {
                throw new Exception("Invalid entity id");
            }

            string path = string.Empty;

            switch (entityType)
            {
                case AppConstants.MediaEntityType.User:
                    path = $"/uploads/users/{entityId}/profile_{Guid.NewGuid()}.jpg";
                    break;
                case AppConstants.MediaEntityType.Post:
                    path = $"/uploads/posts/{entityId}/{Guid.NewGuid()}.jpg";
                    break;
                default:
                    throw new Exception("Invalid entity type");
            }

            return path;
        }

        public async Task DeleteMediaFileAsync(long entityId, AppConstants.MediaEntityType entityType)
        {
            if (entityId <= 0)
            {
                throw new Exception("Invalid entity id");
            }
            MediaFile? mediaFile = await _dbContext.MediaFiles.FirstOrDefaultAsync(m => m.EntityType == entityType && m.EntityId == entityId);
            if (mediaFile == null)
            {
                throw new Exception("File doesn't exist");
            }
            string fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", mediaFile.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            _dbContext.MediaFiles.Remove(mediaFile);
            await _dbContext.SaveChangesAsync();
            try
            {
                await _inMemoryDb.KeyDeleteAsync(InMemoryUtils.GetKey(AppConstants.InMemoryOperationType.ADD_MEDIA_FILE_PATH, entityType, entityId));
            }
            catch (Exception ex) { }
        }
    }
}
