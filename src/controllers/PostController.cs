using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace PostCommentApi.Controllers
{
  [ApiController]
  [Route("api/{username}/posts")]
  public class PostsController : ControllerBase
  {
    // private readonly AppDb _db;
    private readonly IPostService _postService;
    public PostsController(IPostService postService)
    {
      _postService = postService;
    }
    [HttpGet]
    public async Task<IActionResult> GetLatestPosts()
    {
      var latest = await _postService.GetNextPostsFromId(int.MaxValue);
      return Ok(latest);
    }
    [HttpGet("next/{lastPostId}")]
    public async Task<IActionResult> GetNextPosts(int lastPostId)
    {
      var posts = await _postService.GetNextPostsFromId(lastPostId);
      return Ok(posts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
      var post = await _postService.GetPostById(id);
      return Ok(post);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto, string username)
    {
      var post = await _postService.CreatePostForUserName(dto, username);
      return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePostDto dto)
    {
      // Find the post
      await _postService.UpdatePost(id, dto);
      // According to REST semantics, return 204 No Content on successful PUT
      return NoContent();
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
      await _postService.DeletePost(id);
      return NoContent();
    }
  }
}