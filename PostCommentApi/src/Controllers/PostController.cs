using Microsoft.AspNetCore.Mvc;
using PostCommentApi.Dtos;
using PostCommentApi.Services;

namespace PostCommentApi.Controllers;

[ApiController]
[Route("api/{username}/posts")]
public class PostsController(IPostService postService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetLatestPosts()
  {
    var latest = await postService.GetNextPostsFromPostId(int.MaxValue);
    return Ok(latest);
  }

  [HttpGet("next/{lastPostId}")]
  public async Task<IActionResult> GetNextPosts(int lastPostId)
  {
    var posts = await postService.GetNextPostsFromPostId(lastPostId);
    return Ok(posts);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetById(int id)
  {
    var post = await postService.GetPostById(id);
    return Ok(post);
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreatePostDto dto, string username)
  {
    var post = await postService.CreatePostForUserName(dto, username);
    return CreatedAtAction(nameof(GetById), new { id = post.Id, username = username }, post);
  }
  [HttpPut("{id}")]
  public async Task<IActionResult> Update(int id, [FromBody] CreatePostDto dto)
  {
    // Find the post
    await postService.UpdatePost(id, dto);
    // According to REST semantics, return 204 No Content on successful PUT
    return NoContent();
  }
  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(int id)
  {
    await postService.DeletePost(id);
    return NoContent();
  }
}