using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api")]
public class HomeController : ControllerBase
{
  private readonly AppDb _db;
  private readonly IPostService _postService;
  public HomeController(AppDb db, IPostService postService)
  {
    _db = db;
    _postService = postService;
  }


  [HttpGet("feed/{lastId}")]
  public async Task<IActionResult> Feed(int lastId)
  {
    if (lastId == int.MaxValue)
    {
      var latest = await _postService.GetNextPostsFromId(int.MaxValue);
      return Ok(latest);
    }

    var last = await _postService.GetPostById(lastId);
    // If the given lastId doesn't exist, return an empty list (client can handle as end-of-feed)
    if (last == null) return Ok(new List<PostDto>());

    var posts = await _postService.GetNextPostsFromId(lastId);

    return Ok(posts);
  }
  [HttpGet("filter/posts")]
  public async Task<IActionResult> Filter([FromQuery] PostQueryDto query)
  {
    var posts = await _postService.FilterPosts(query);
    return Ok(posts);
  }
}

