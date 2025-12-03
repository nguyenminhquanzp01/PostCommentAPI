using Microsoft.AspNetCore.Mvc;
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
    // Authentication logic would go here
    var token = await authService.Authenticate(request.Username, request.Password);
    Console.WriteLine("Generated Token: " + token);
    return Ok(new { Token = token });
  }

  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] RegisterDto request)
  {
    var token = await authService.Register(request);
    // Return token and basic user identifier via response header or body
    return Created(string.Empty, new { Token = token });
  }
}