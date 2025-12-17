using System.Text.Json;
using Amazon.S3.Model;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PostCommentApi.Dtos;
using PostCommentApi.Entities;
using PostCommentApi.Exceptions;
using StackExchange.Redis;

namespace PostCommentApi.Services;

public class CommentService(AppDb db, IMapper mapper, IDatabase redis) : ICommentService
{
  /// <summary>
  /// Create a new comment under the given post. The comment's author is taken from the caller's token (<paramref name="currentUserId"/>).
  /// If <paramref name="dto"/> contains a ParentId, the parent is validated to belong to the same post.
  /// Nested comments deeper than 2 levels are attached to the parent's parent to keep tree depth bounded.
  /// </summary>
  /// <param name="postId">Target post id.</param>
  /// <param name="dto">CreateCommentDto containing content and optional ParentId.</param>
  /// <param name="currentUserId">Id of the caller (from token) used as author.</param>
  /// <param name="isAdmin">Flag indicating whether caller is an admin (not used for creation).</param>
  /// <returns>The created CommentDto.</returns>
  /// <exception cref="NotFoundException">Thrown when the post or parent comment doesn't exist.</exception>
  public async Task<CommentDto> CreateCommentForPost(int postId, CreateCommentDto dto, int currentUserId, bool isAdmin)
  {
    int getCommentLevel(int? id)
    {
      int level = 0;
      var comment = db.Comments.Find(id);
      while (comment?.ParentId != null)
      {
        level++;
        comment = db.Comments.Find(comment.ParentId);
      }
      return level;
    }
    var post = await db.Posts.FindAsync(postId) ?? throw new NotFoundException("Post", postId);
    if (dto.ParentId is not null)
    {
      var parent = await db.Comments.FindAsync(dto.ParentId.Value);
      if (parent == null || parent.PostId != postId) throw new NotFoundException("Parent Comment", dto.ParentId.Value);
    }
    var comment = mapper.Map<Comment>(dto);
    var level = getCommentLevel(dto.ParentId);
    if (level >= 2)
      comment.ParentId = db.Comments.Find(dto.ParentId.Value).ParentId;
    comment.PostId = postId;
    comment.CreatedAt = DateTime.UtcNow;

    // Author is always taken from the caller's token (currentUserId). Do not accept author in the DTO.
    comment.UserId = currentUserId;

    db.Comments.Add(comment);
    await db.SaveChangesAsync();
    return mapper.Map<CommentDto>(comment);
  }


  /// <summary>
  /// Delete a comment by id. Only the comment owner or an admin may delete.
  /// </summary>
  /// <param name="commentId">Id of the comment to delete.</param>
  /// <param name="currentUserId">Id of the caller (from token) used to verify ownership.</param>
  /// <param name="isAdmin">If true, caller is treated as admin and bypasses ownership check.</param>
  /// <returns>Task representing the async operation.</returns>
  /// <exception cref="NotFoundException">Thrown when the comment does not exist or caller is unauthorized.</exception>
  public async Task DeleteComment(int commentId, int currentUserId, bool isAdmin)
  {
    var comment = await db.Comments.FindAsync(commentId) ?? throw new NotFoundException("Comment", commentId);

    // Only admin or owner can delete
    if (!isAdmin && comment.UserId != currentUserId)
      throw new NotFoundException("Comment", commentId); // hide existence for unauthorized

    db.Comments.Remove(comment);
    await db.SaveChangesAsync();
  }

  /// <summary>
  /// Get a flat list of comments for the given post ordered by creation time.
  /// </summary>
  /// <param name="postId">Post id to query comments for.</param>
  /// <returns>Enumerable of CommentDto ordered by CreatedAt ascending.</returns>
  /// <exception cref="NotFoundException">Thrown when the post does not exist.</exception>
  public async Task<IEnumerable<CommentDto>> GetCommentsForPostId(int postId)
  {
    var post = await db.Posts.FindAsync(postId) ?? throw new NotFoundException("Post", postId);
    var comments = await db.Comments
      .Where(c => c.PostId == postId)
      .OrderBy(c => c.CreatedAt)
      .Select(c => mapper.Map<CommentDto>(c))
      .ToListAsync();
    return comments;
  }

  /// <summary>
  /// Build and return the comment tree for a post. The result is cached for a short period.
  /// Replies are nested under their parent comment. Cycle detection is used to avoid infinite recursion.
  /// </summary>
  /// <param name="postId">Post id.</param>
  /// <returns>Nested CommentTreeDto list representing the root comments and their replies.</returns>
  /// <exception cref="NotFoundException">Thrown when the post does not exist.</exception>
  public async Task<IEnumerable<CommentTreeDto>> GetCommentTreeForPostId(int postId)
  {
    var cacheKey = $"comments:tree:{postId}";
    var cache = await redis.StringGetAsync(cacheKey);
    if (cache.HasValue)
    {
      var cachedTree = JsonSerializer.Deserialize<List<CommentTreeDto>>(cache.ToString());
      if (cachedTree != null)
        return cachedTree;
    }

    var exists = await db.Posts.AnyAsync(p => p.Id == postId);
    if (!exists) throw new NotFoundException("Post", postId);


    var comments = await db.Comments
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
    List<CommentTreeDto> Build2(int? parentId, HashSet<int>? ancestors = null)
    {
      var d = new Dictionary<int, CommentTreeDto>();
      d.Add(0, new CommentTreeDto { Id = 0 });
      foreach (var c in comments)
      {
        var node = new CommentTreeDto
        {
          Id = c.Id,
          ParentId = c.ParentId,
          Content = c.Content,
          CreatedAt = c.CreatedAt,
          Replies = new List<CommentTreeDto>()
        };
        d[c.Id] = node;
      }
      foreach (var i in d)
      {
        if (i.Key != 0)
          d[i.Value.ParentId ?? 0].Replies.Add(i.Value);
      }
      var res = d.Where(kv => kv.Value.ParentId == null).Select(kv => kv.Value).ToList();
      return res;
    }
    var tree = Build2(null);
    var serializedTree = JsonSerializer.Serialize(tree);
    await redis.StringSetAsync(cacheKey, serializedTree, TimeSpan.FromMinutes(5));

    return tree;
  }
  /// <summary>
  /// Update an existing comment's content. Only the owner or an admin may update.
  /// </summary>
  /// <param name="commentId">Id of the comment to update.</param>
  /// <param name="dto">UpdateCommentDto containing new content.</param>
  /// <param name="currentUserId">Caller id for ownership check.</param>
  /// <param name="isAdmin">Whether caller is global admin.</param>
  /// <returns>The updated CommentDto.</returns>
  /// <exception cref="NotFoundException">Thrown when comment isn't found or unauthorized.</exception>
  public async Task<CommentDto> UpdateComment(int commentId, UpdateCommentDto dto, int currentUserId, bool isAdmin)
  {
    var comment = await db.Comments.FindAsync(commentId) ?? throw new NotFoundException("Comment", commentId);

    // Only admin or owner can update
    if (!isAdmin && comment.UserId != currentUserId)
      throw new NotFoundException("Comment", commentId);

    comment.Content = dto.Content;
    await db.SaveChangesAsync();
    return mapper.Map<CommentDto>(comment);
  }

  // ...existing code...
  /// <summary>
  /// Get up to 10 previous comments at the same level as the comment identified by <paramref name="lastCommentId"/>.
  /// If <paramref name="lastCommentId"/> equals <see cref="int.MaxValue"/>, returns the latest top-level comments and caches them.
  /// </summary>
  /// <param name="postId">Post id.</param>
  /// <param name="lastCommentId">Last comment id marker; use int.MaxValue to request latest top-level comments.</param>
  /// <returns>Enumerable of CommentDto (max 10) ordered from newest to oldest among the previous items.</returns>
  public async Task<IEnumerable<CommentDto>> GetPreviousCommentsFromIdForPost(int postId, int lastCommentId)
  {
    // If lastCommentId is int.MaxValue, return the latest top-level comments and cache them
    if (lastCommentId == int.MaxValue)
    {
      var cacheKey = $"comments:top:{postId}";
      var cache = await redis.StringGetAsync(cacheKey);
      if (cache.HasValue)
      {
        var cachedComments = JsonSerializer.Deserialize<List<CommentDto>>(cache.ToString());
        if (cachedComments != null)
          return cachedComments;
      }

      var topLevelComments = await db.Comments
        .Where(c => c.PostId == postId && c.ParentId == null)
        .OrderByDescending(c => c.CreatedAt)
        .Take(10)
        .Select(c => mapper.Map<CommentDto>(c))
        .ToListAsync();

      var serializedComments = JsonSerializer.Serialize(topLevelComments);
      await redis.StringSetAsync(cacheKey, serializedComments, TimeSpan.FromMinutes(1));

      return topLevelComments;
    }

    var lastComment = await db.Comments.FindAsync(lastCommentId);
    if (lastComment == null || lastComment.PostId != postId)
      throw new NotFoundException("Comment", lastCommentId);

    var comments = await db.Comments
      .Where(c => c.PostId == postId && c.ParentId == lastComment.ParentId && c.CreatedAt < lastComment.CreatedAt)
      .OrderByDescending(c => c.CreatedAt)
      .Take(10)
      .Select(c => mapper.Map<CommentDto>(c))
      .ToListAsync();

    return comments;
  }
  /// <summary>
  /// Return the total comment count for a post. The value is cached for short duration.
  /// </summary>
  /// <param name="postId">Post id.</param>
  /// <returns>Total number of comments for the post.</returns>
  public async Task<int> GetCommentCountForPost(int postId)
  {
    var cacheKey = $"comments:count:{postId}";
    var cache = await redis.StringGetAsync(cacheKey);
    if (cache.HasValue)
    {
      if (int.TryParse(cache, out var cnt))
        return cnt;
    }

    var count = await db.Comments.CountAsync(c => c.PostId == postId);
    await redis.StringSetAsync(cacheKey, count.ToString(), TimeSpan.FromMinutes(10));

    return count;
  }

}