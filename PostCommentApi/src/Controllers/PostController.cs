using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCommentApi.Dtos;
using PostCommentApi.Services;
using System.Security.Claims;

namespace PostCommentApi.Controllers;

[ApiController]
[Route("api/posts")]
public class PostsController(IPostService postService) : ControllerBase
{
  [HttpGet]
  [AllowAnonymous]
  /// <summary>
  /// Return the latest posts (page size = 10).
  /// </summary>
  /// <returns>List of PostDto ordered by CreatedAt descending.</returns>
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> GetLatestPosts()
  {
    var latest = await postService.GetPreviousPostsFromPostId(int.MaxValue);
    return Ok(latest);
  }

  [HttpGet("next/{lastPostId}")]
  [AllowAnonymous]
  /// <summary>
  /// Return older posts before the specified post id marker.
  /// </summary>
  /// <param name="lastPostId">Id of the last post currently shown; use this to paginate older posts.</param>
  /// <returns>List of PostDto (up to page size).</returns>
  public async Task<IActionResult> GetOlderPosts(int lastPostId)
  {
    var posts = await postService.GetPreviousPostsFromPostId(lastPostId);
    return Ok(posts);
  }

  [HttpGet("{id}")]
  [AllowAnonymous]
  /// <summary>
  /// Get a post by id.
  /// </summary>
  /// <param name="id">Post id.</param>
  /// <returns>PostDto.</returns>
  public async Task<IActionResult> GetById(int id)
  {
    var post = await postService.GetPostById(id);
    return Ok(post);
  }

  [HttpPost]
  [Authorize]
  /// <summary>
  /// Create a new post authored by the caller.
  /// </summary>
  /// <param name="dto">CreatePostDto payload.</param>
  /// <returns>Created PostDto with 201 status.</returns>
  public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
  {
    // Determine caller
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var callerUserName = User.FindFirst(ClaimTypes.Name)?.Value;
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    if (!int.TryParse(userIdClaim, out var callerId))
      return Forbid();

    var postForUser = await postService.CreatePost(dto, callerId);
    return CreatedAtAction(nameof(GetById), new { id = postForUser.Id }, postForUser);
  }

  [HttpPut("{id}")]
  [Authorize]
  /// <summary>
  /// Update a post. Only the owner or an admin can update.
  /// </summary>
  /// <param name="id">Post id to update.</param>
  /// <param name="dto">CreatePostDto containing updated fields.</param>
  /// <returns>204 NoContent on success.</returns>
  public async Task<IActionResult> Update(int id, [FromBody] CreatePostDto dto)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    await postService.UpdatePost(id, dto, callerId, isAdmin);
    return NoContent();
  }

  [HttpDelete("{id}")]
  [Authorize]
  /// <summary>
  /// Delete a post. Only the owner or an admin may delete.
  /// </summary>
  /// <param name="id">Post id to delete.</param>
  /// <returns>204 NoContent on success.</returns>
  public async Task<IActionResult> Delete(int id)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    await postService.DeletePost(id, callerId, isAdmin);
    return NoContent();
  }
}