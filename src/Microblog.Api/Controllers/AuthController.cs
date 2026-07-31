namespace Microblog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService, IConfiguration configuration)
    : ControllerBase
{
    [RateLimit(AppConstants.ApiRequestAction.Register)]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
    {
        var response = new CommonUtils.ControllerResponseParams();

        var newUser = await authService.RegisterAsync(request.Username, request.Email, request.Password);
        if (newUser == null) throw new Exception("Unable to register user");

        response.Success = true;
        response.Message = "User registered successfully";
        response.Data = newUser;

        return Ok(response);
    }

    [RateLimit(AppConstants.ApiRequestAction.Login)]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto request)
    {
        var response = new CommonUtils.ControllerResponseParams();

        dynamic? loginObj = await authService.LoginAsync(request.Username, request.Email, request.Password);
        if (loginObj == null) throw new Exception("Unable to login");
        Response.Cookies.Append("accessToken", loginObj.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(Convert.ToDouble(configuration["Jwt:ExpireMinutes"]))
        });
        Response.Cookies.Append("refreshToken", loginObj.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(Convert.ToDouble(configuration["Jwt:RefreshTokenExpireDays"]))
        });

        response.Success = true;
        response.Message = "Login successful";
        response.Data = loginObj.User;

        return Ok(response);
    }

    [RateLimit(AppConstants.ApiRequestAction.Login)]
    [HttpPost("refreshtoken")]
    public async Task<IActionResult> RefreshToken()
    {
        var response = new CommonUtils.ControllerResponseParams();
        string? token = Request.Cookies["refreshToken"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            var authToken = await authService.RefreshAccessTokenAsync(token);
            if (authToken == null) throw new Exception("Invalid refresh token");
            Response.Cookies.Append("refreshToken", authToken.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        response.Success = true;
        response.Message = "Token refreshed successfully";
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var response = new CommonUtils.ControllerResponseParams();
        string refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(refreshToken)) await authService.Logout(refreshToken);
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        response.Success = true;
        response.Message = "Logged out successfully";
        return Ok(response);
    }
}
