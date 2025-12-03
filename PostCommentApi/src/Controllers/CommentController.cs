using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PostCommentApi.Dtos;
using PostCommentApi.Services;

namespace PostCommentApi.Controllers;

[ApiController]
[Route("api/posts/{postId}/comments")]
public class CommentController(ICommentService commentService) : ControllerBase
{
  // Flat comments for a post
  [HttpGet("flat")]
  public async Task<IActionResult> GetCommentsFlat(int postId)
  {
    var comments = await commentService.GetCommentsForPostId(postId);
    return Ok(comments);
  }

  // Create a comment on a post
  [HttpPost("")]
  [Authorize]
  public async Task<IActionResult> CreateComment(int postId, [FromBody] CreateCommentDto dto)
  {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    var result = await commentService.CreateCommentForPost(postId, dto, callerId, isAdmin);
    return CreatedAtAction(nameof(GetCommentsFlat), new { postId }, result);
  }

  [HttpGet("tree")]
  public async Task<IActionResult> GetCommentsTree(int postId)
  {
    var tree = await commentService.GetCommentTreeForPostId(postId);
    return Ok(tree);
  }
  [HttpDelete("{commentId}")]
  [Authorize]
  public async Task<IActionResult> DeleteComment(int commentId)
  {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    await commentService.DeleteComment(commentId, callerId, isAdmin);
    return NoContent();
  }
  [HttpPut("{commentId}")]
  [Authorize]
  public async Task<IActionResult> UpdateComment(int commentId, [FromBody] UpdateCommentDto dto)
  {
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var callerId))
      return Forbid();
    var isAdmin = bool.TryParse(User.FindFirst("isAdmin")?.Value, out var adminFlag) && adminFlag;

    var updatedComment = await commentService.UpdateComment(commentId, dto, callerId, isAdmin);
    return Ok(updatedComment);
  }
}