using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace PostCommentApi.Controllers
{
  [ApiController]
  [Route("api/posts")]
  public class PostsController : ControllerBase
  {
    private readonly AppDb _db;
    public PostsController(AppDb db) => _db = db;


    [HttpGet]
    public async Task<IEnumerable<PostDto>> GetAll()
    {
      return await _db.Posts
      .OrderByDescending(p => p.CreatedAt).Take(50)
      .Select(p => new PostDto { Id = p.Id, Title = p.Title, Content = p.Content, CreatedAt = p.CreatedAt })
      .ToListAsync();
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
      var p = await _db.Posts.FindAsync(id);
      if (p == null) return NotFound();
      return Ok(new PostDto { Id = p.Id, Title = p.Title, Content = p.Content, CreatedAt = p.CreatedAt });
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
    {
      int userIdTest = 1;
      var post = new Post { UserId = userIdTest, Title = dto.Title, Content = dto.Content };
      _db.Posts.Add(post);
      await _db.SaveChangesAsync();
      return CreatedAtAction(nameof(Get), new { id = post.Id }, new PostDto { Id = post.Id, Title = post.Title, Content = post.Content, CreatedAt = post.CreatedAt });
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePostDto dto)
    {
      // Find the post
      var post = await _db.Posts.FindAsync(id);
      if (post == null) return NotFound();

      post.Title = dto.Title;
      post.Content = dto.Content;
      await _db.SaveChangesAsync();

      // According to REST semantics, return 204 No Content on successful PUT
      return NoContent();
    }

    // Flat comments for a post
    [HttpGet("{id}/comments/flat")]
    public async Task<IActionResult> GetCommentsFlat(int id)
    {
      var exists = await _db.Posts.AnyAsync(p => p.Id == id);
      if (!exists) return NotFound();

      var comments = await _db.Comments
        .Where(c => c.PostId == id)
        .OrderBy(c => c.CreatedAt)
        .Select(c => new CommentFlatDto { Id = c.Id, ParentId = c.ParentId, Content = c.Content, CreatedAt = c.CreatedAt })
        .ToListAsync();

      return Ok(comments);
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> CreateComment(int id, [FromBody] CreateCommentDto dto)
    {
      var post = await _db.Posts.FindAsync(id);
      if (post == null) return NotFound();

      if (dto.ParentId is not null)
      {
        var parent = await _db.Comments.FindAsync(dto.ParentId.Value);
        if (parent == null || parent.PostId != id) return BadRequest("Invalid parent comment");
      }

      // For now use a test user id
      int userIdTest = 1;

      var comment = new Comment
      {
        PostId = id,
        ParentId = dto.ParentId,
        Content = dto.Content,
        UserId = userIdTest
      };

      _db.Comments.Add(comment);
      await _db.SaveChangesAsync();

      var result = new CommentFlatDto { Id = comment.Id, ParentId = comment.ParentId, Content = comment.Content, CreatedAt = comment.CreatedAt };

      return CreatedAtAction(nameof(GetCommentsFlat), new { id = id }, result);
    }


    // Tree comments
    [HttpGet("{id}/comments/tree")]
    public async Task<IActionResult> GetCommentsTree(int id)
    {
      var exists = await _db.Posts.AnyAsync(p => p.Id == id);
      if (!exists) return NotFound();


      var comments = await _db.Comments
      .Where(c => c.PostId == id)
      .OrderBy(c => c.CreatedAt)
      .ToListAsync();

      var lookup = comments.ToLookup(c => c.ParentId);

      List<CommentTreeDto> Build(int? parentId)
      {
        return lookup[parentId].Select(c => new CommentTreeDto
        {
          Id = c.Id,
          ParentId = c.ParentId,
          Content = c.Content,
          CreatedAt = c.CreatedAt,
          Replies = Build(c.Id)
        }).ToList();
      }


      var tree = Build(null);
      return Ok(tree);
    }
  }
}