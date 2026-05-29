namespace Microblog.Api.DTOs;

public class CreateUpdateCommentRequestDto
{
    public long PostId { get; set; }
    public long CommentId { get; set; }
    public string Content { get; set; } = string.Empty;
}