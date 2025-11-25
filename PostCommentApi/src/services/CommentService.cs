
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

public class CommentService : ICommentService
{
  private readonly AppDb _db;
  private readonly IDistributedCache _cache;
  private readonly IMapper _mapper;
  public CommentService(AppDb db, IDistributedCache cache, IMapper mapper)
  {
    _db = db;
    _cache = cache;
    _mapper = mapper;
  }

  public async Task<CommentDto> CreateCommentForPost(int postId, CreateCommentDto dto)
  {
    var post = await _db.Posts.FindAsync(postId) ?? throw new NotFoundException("Post", postId);
    if (dto.ParentId is not null)
    {
      var parent = await _db.Comments.FindAsync(dto.ParentId.Value);
      if (parent == null || parent.PostId != postId) throw new Exception("Unhandled Invalid parent comment");
    }
    var comment = _mapper.Map<Comment>(dto);
    comment.PostId = postId;
    comment.CreatedAt = DateTime.UtcNow;
    _db.Comments.Add(comment);
    await _db.SaveChangesAsync();
    return _mapper.Map<CommentDto>(comment);
  }


  public async Task DeleteComment(int commentId)
  {
    var comment = await _db.Comments.FindAsync(commentId) ?? throw new NotFoundException("Comment", commentId);
    _db.Comments.Remove(comment);
    await _db.SaveChangesAsync();
  }

  public async Task<IEnumerable<CommentDto>> GetCommentsForPostId(int postId)
  {
    var post = await _db.Posts.FindAsync(postId) ?? throw new NotFoundException("Post", postId);
    var comments = await _db.Comments
        .Where(c => c.PostId == postId)
        .OrderBy(c => c.CreatedAt)
        .Select(c => _mapper.Map<CommentDto>(c))
        .ToListAsync();
    return comments;
  }

  public async Task<IEnumerable<CommentTreeDto>> GetCommentTreeForPostId(int postId)
  {
    var exists = await _db.Posts.AnyAsync(p => p.Id == postId);
    if (!exists) throw new NotFoundException("Post", postId);


    var comments = await _db.Comments
    .Where(c => c.PostId == postId)
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
    return tree;
  }

  public async Task<CommentDto> UpdateComment(int commentId, UpdateCommentDto dto)
  {
    var comment = await _db.Comments.FindAsync(commentId) ?? throw new NotFoundException("Comment", commentId);
    comment.Content = dto.Content;
    await _db.SaveChangesAsync();
    return _mapper.Map<CommentDto>(comment);
  }
}