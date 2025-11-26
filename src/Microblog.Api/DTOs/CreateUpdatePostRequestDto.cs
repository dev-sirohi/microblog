namespace Microblog.Api.DTOs
{
    public class CreateUpdatePostRequestDto
    {
        public long PostId { get; set; }
        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
