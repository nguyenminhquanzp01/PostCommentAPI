using Microsoft.AspNetCore.Mvc;
using PostCommentApi.Dtos;
using PostCommentApi.Services;

namespace PostCommentApi.Controllers;

[ApiController]
[Route("api/{username}/posts/{postId}/comments")]
public class CommentController(CommentService commentService) : ControllerBase
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
  public async Task<IActionResult> CreateComment(int postId, int username, [FromBody] CreateCommentDto dto)
  {

    var result = await commentService.CreateCommentForPost(postId, dto);
    return CreatedAtAction(nameof(GetCommentsFlat), new { postId }, result);
  }
  
  [HttpGet("tree")]
  public async Task<IActionResult> GetCommentsTree(int postId)
  {
    var tree = await commentService.GetCommentTreeForPostId(postId);
    return Ok(tree);
  }
  [HttpDelete("{commentId}")]
  public async Task<IActionResult> DeleteComment(int commentId)
  {
    await commentService.DeleteComment(commentId);
    return NoContent();
  }
  [HttpPut("{commentId}")]
  public async Task<IActionResult> UpdateComment(int commentId, [FromBody] UpdateCommentDto dto)
  {
    var updatedComment = await commentService.UpdateComment(commentId, dto);
    return Ok(updatedComment);
  }
}