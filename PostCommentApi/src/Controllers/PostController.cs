using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCommentApi.Dtos;
using PostCommentApi.Services;

namespace PostCommentApi.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController(IPostService postService) : ControllerBase
{
  [HttpGet]
  [AllowAnonymous]
  public async Task<IActionResult> GetLatestPosts()
  {
    var latest = await postService.GetNextPostsFromPostId(int.MaxValue);
    return Ok(latest);
  }

  [HttpGet("next/{lastPostId}")]
  [AllowAnonymous]
  public async Task<IActionResult> GetNextPosts(int lastPostId)
  {
    var posts = await postService.GetNextPostsFromPostId(lastPostId);
    return Ok(posts);
  }

  [HttpGet("{id}")]
  [AllowAnonymous]
  public async Task<IActionResult> GetById(int id)
  {
    var post = await postService.GetPostById(id);
    return Ok(post);
  }

  [HttpPost]
  [Authorize]
  public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
  {
    // Determine caller
    var userIdClaim = User.FindFirst("sub")?.Value;
    var callerUserName = User.FindFirst("unique_name")?.Value;
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    if (!int.TryParse(userIdClaim, out var callerId))
      return Forbid();

    var postForUser = await postService.CreatePost(dto, callerId);
    return CreatedAtAction(nameof(GetById), new { id = postForUser.Id, username = callerUserName }, postForUser);
  }

  [HttpPut("{id}")]
  [Authorize]
  public async Task<IActionResult> Update(int id, [FromBody] CreatePostDto dto)
  {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    await postService.UpdatePost(id, dto, callerId, isAdmin);
    return NoContent();
  }

  [HttpDelete("{id}")]
  [Authorize]
  public async Task<IActionResult> Delete(int id)
  {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    await postService.DeletePost(id, callerId, isAdmin);
    return NoContent();
  }
}