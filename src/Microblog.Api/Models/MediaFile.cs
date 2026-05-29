namespace Microblog.Api.Models;

public class MediaFile
{
    public long Id { get; set; }
    public long EntityId { get; set; }
    public AppConstants.MediaEntityType EntityType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}