using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCommentApi.Dtos;
using PostCommentApi.Services;
using System.Security.Claims;

namespace PostCommentApi.Controllers;

[ApiController]
[Route("api/pages")]
public class PageController(IPageService pageService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetPages()
  {
    var pages = await pageService.GetPages();
    return Ok(pages);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetPageById(int id)
  {
    var page = await pageService.GetPageById(id);
    return Ok(page);
  }

  [HttpGet("{id}/posts")]
  public async Task<IActionResult> GetPostsForPage(int id)
  {
    var posts = await pageService.GetPostsForPage(id);
    return Ok(posts);
  }

  [HttpPost]
  [Authorize]
  public async Task<IActionResult> CreatePage([FromBody] CreatePageDto dto)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    var page = await pageService.CreatePage(dto, callerId);
    return CreatedAtAction(nameof(GetPageById), new { id = page.Id }, page);
  }

  [HttpPut("{id}")]
  [Authorize]
  public async Task<IActionResult> UpdatePage(int id, [FromBody] UpdatePageDto dto)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    var updatedPage = await pageService.UpdatePage(id, dto, callerId, isAdmin);
    return Ok(updatedPage);
  }

  [HttpDelete("{id}")]
  [Authorize]
  public async Task<IActionResult> DeletePage(int id)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    await pageService.DeletePage(id, callerId, isAdmin);
    return NoContent();
  }

  [HttpPost("{id}/follow")]
  [Authorize]
  public async Task<IActionResult> FollowPage(int id)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();

    await pageService.FollowPage(id, callerId);
    return NoContent();
  }

  [HttpPost("{id}/posts")]
  [Authorize]
  public async Task<IActionResult> CreatePostInPage(int id, [FromBody] CreatePostDto dto)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();

    var post = await pageService.CreatePostInPage(id, dto, callerId);
    return CreatedAtAction("GetById", "Posts", new { id = post.Id }, post);
  }

  [HttpPut("{pageId}/users/{userId}/role")]
  [Authorize]
  public async Task<IActionResult> ChangeUserRole(int pageId, int userId, [FromBody] ChangeRoleDto dto)
  {
    var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim, out var currentUserId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    await pageService.ChangeUserRole(pageId, userId, dto.NewRole, currentUserId, isAdmin);
    return NoContent();
  }
}