using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCommentApi.Dtos;
using PostCommentApi.Services;

namespace PostCommentApi.Controllers;

[ApiController]
[Route("api")]
public class HomeController(IPostService postService, ILogger<HomeController> logger) : ControllerBase
{
  [HttpGet("feed/{lastId}")]
  public async Task<IActionResult> Feed(int lastId)
  {
    if (lastId == int.MaxValue)
    {
      var latest = await postService.GetNextPostsFromPostId(int.MaxValue);
      return Ok(latest);
    }

    var last = await postService.GetPostById(lastId);
    // If the given lastId doesn't exist, return an empty list (client can handle as end-of-feed)
    if (last == null) return Ok(new List<PostDto>());

    var posts = await postService.GetNextPostsFromPostId(lastId);

    return Ok(posts);
  }
  [HttpGet("filter/posts")]
  public async Task<IActionResult> Filter([FromQuery] PostQueryDto query)
  {
    var posts = await postService.FilterPosts(query);
    return Ok(posts);
  }
  [Authorize]
  [HttpGet("/")]
  public IActionResult GetRoot()
  {
    logger.LogInformation("API is running.");
    return  Ok("PostCommentApi is running.");
  }
}