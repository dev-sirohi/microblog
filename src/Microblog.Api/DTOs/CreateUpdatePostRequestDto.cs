namespace Microblog.Api.DTOs;

public class CreateUpdatePostRequestDto
{
    [Required] public string Content { get; set; } = string.Empty;
}