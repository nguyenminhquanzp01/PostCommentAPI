using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PostCommentApi.Services;
using PostCommentApi.Dtos;

namespace PostCommentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginDto request)
  {
    var result = await authService.Authenticate(request.Username, request.Password);

    // Set refresh token in an HttpOnly cookie
    var cookieOptions = new CookieOptions
    {
      HttpOnly = true,
      Secure = true, // set to false if you're testing on http locally
      SameSite = SameSiteMode.Strict,
      Expires = result.RefreshTokenExpiresAtUtc
    };
    Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);

    return Ok(new { AccessToken = result.AccessToken });
  }

  [HttpPost("refresh")]
  public async Task<IActionResult> Refresh([FromBody] PostCommentApi.Dtos.RefreshRequestDto request)
  {
    // Read the refresh token from the HttpOnly cookie only
    var providedRefresh = Request.Cookies["refreshToken"];
    if (string.IsNullOrEmpty(providedRefresh)) return Unauthorized();

    // Access token may be provided in the body; if not, try Authorization header
    var accessToken = request?.AccessToken;
    if (string.IsNullOrEmpty(accessToken))
    {
      var authHeader = Request.Headers["Authorization"].ToString();
      if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
      {
        accessToken = authHeader.Substring("Bearer ".Length).Trim();
      }
    }

    if (string.IsNullOrEmpty(accessToken)) return BadRequest("Access token is required.");

    var newPair = await authService.RefreshTokens(accessToken, providedRefresh);

    // rotate cookie with the new refresh token
    var cookieOptions = new CookieOptions
    {
      HttpOnly = true,
      Secure = true,
      SameSite = SameSiteMode.Strict,
      Expires = newPair.RefreshTokenExpiresAtUtc
    };
    Response.Cookies.Append("refreshToken", newPair.RefreshToken, cookieOptions);

    return Ok(new { AccessToken = newPair.AccessToken });
  }

  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterDto request)
  {
    var token = await authService.Register(request);
    // Return token and basic user identifier via response header or body
    return Created(string.Empty, new { Token = token });
  }
}