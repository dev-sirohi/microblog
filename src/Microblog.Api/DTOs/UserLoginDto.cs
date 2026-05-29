namespace Microblog.Api.DTOs;

public class UserLoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}