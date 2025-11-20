using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


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

      // Update fields
      post.Title = dto.Title;
      post.Content = dto.Content;

      // Persist changes
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

      var swFlat = Stopwatch.StartNew();
      var comments = await _db.Comments
        .Where(c => c.PostId == id)
        .OrderBy(c => c.CreatedAt)
        .Select(c => new CommentFlatDto { Id = c.Id, ParentId = c.ParentId, Content = c.Content, CreatedAt = c.CreatedAt })
        .ToListAsync();
      swFlat.Stop();
      Console.WriteLine($"[GetCommentsFlat] Query took {swFlat.Elapsed.TotalMilliseconds:F2} ms for post {id}");

      return Ok(comments);
    }

    // Create a comment on a post
    [HttpPost("{id}/comments")]
    public async Task<IActionResult> CreateComment(int id, [FromBody] CreateCommentDto dto)
    {
      var post = await _db.Posts.FindAsync(id);
      if (post == null) return NotFound();

      // If parent is provided, ensure it exists and belongs to the same post
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


      var swTree = Stopwatch.StartNew();
      var comments = await _db.Comments
      .Where(c => c.PostId == id)
      .OrderBy(c => c.CreatedAt)
      .ToListAsync();


      // Build lookup keyed by nullable ParentId so root (null) can be used directly
      var lookup = comments.ToLookup(c => c.ParentId);

      // Safe recursive builder with cycle detection to avoid infinite recursion
      List<CommentTreeDto> Build(int? parentId, HashSet<int>? ancestors = null)
      {
        var currentAncestors = ancestors ?? new HashSet<int>();

        // If parentId is in ancestors, we've detected a cycle; abort to prevent infinite recursion
        if (parentId.HasValue && currentAncestors.Contains(parentId.Value))
          return new List<CommentTreeDto>();

        return lookup[parentId].Select(c =>
        {
          // Prepare ancestor set for child's recursion: copy and add the current parent id
          // (ancestors should represent the path up to the parent, not include the child itself).
          var childAncestors = new HashSet<int>(currentAncestors);
          if (parentId.HasValue) childAncestors.Add(parentId.Value);
          var res = new CommentTreeDto
          {
            Id = c.Id,
            ParentId = c.ParentId,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            Replies = Build(c.Id, childAncestors)
          };
          return res;
        }
        ).ToList();

      }
      var tree = Build(null);
      swTree.Stop();
      Console.WriteLine($"[GetCommentsTree] Query took {swTree.Elapsed.TotalMilliseconds:F2} ms for post {id}");
      return Ok(tree);
    }
  }
}