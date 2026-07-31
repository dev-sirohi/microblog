namespace Microblog.Api.DTOs;

public class UserRegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UserLoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UserResponseDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateUpdatePostRequestDto
{
    [Required] public string Content { get; set; } = string.Empty;
}
