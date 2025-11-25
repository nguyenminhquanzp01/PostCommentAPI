using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/{username}/posts/{postId}/comments")]
public class CommentController : ControllerBase
{
  private readonly CommentService _commentService;
  public CommentController(CommentService svc) => _commentService = svc;

  // Flat comments for a post
  [HttpGet("flat")]
  public async Task<IActionResult> GetCommentsFlat(int postId)
  {
    var comments = await _commentService.GetCommentsForPostId(postId);
    return Ok(comments);
  }

  // Create a comment on a post
  [HttpPost("")]
  public async Task<IActionResult> CreateComment(int postId, int username, [FromBody] CreateCommentDto dto)
  {

    var result = await _commentService.CreateCommentForPost(postId, dto);
    return CreatedAtAction(nameof(GetCommentsFlat), new { postId = postId }, result);
  }

  // Tree comments
  [HttpGet("tree")]
  public async Task<IActionResult> GetCommentsTree(int postId)
  {
    var tree = await _commentService.GetCommentTreeForPostId(postId);
    return Ok(tree);
  }
  [HttpDelete("{commentId}")]
  public async Task<IActionResult> DeleteComment(int commentId)
  {
    await _commentService.DeleteComment(commentId);
    return NoContent();
  }
  [HttpPut("{commentId}")]
  public async Task<IActionResult> UpdateComment(int commentId, [FromBody] UpdateCommentDto dto)
  {
    var updatedComment = await _commentService.UpdateComment(commentId, dto);
    return Ok(updatedComment);
  }
}