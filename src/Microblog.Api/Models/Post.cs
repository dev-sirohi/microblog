namespace Microblog.Api.Models;

public class Post
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Raw float[] embedding serialized as bytes (1536 floats = 6144 bytes for text-embedding-3-small).
    /// Null until the background embedding job runs after post creation.
    /// </summary>
    public byte[]? EmbeddingData { get; set; }
}
