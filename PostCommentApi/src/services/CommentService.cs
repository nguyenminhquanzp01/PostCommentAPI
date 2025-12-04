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


  public async Task DeleteComment(int commentId, int currentUserId, bool isAdmin)
  {
    var comment = await db.Comments.FindAsync(commentId) ?? throw new NotFoundException("Comment", commentId);

    // Only admin or owner can delete
    if (!isAdmin && comment.UserId != currentUserId)
      throw new NotFoundException("Comment", commentId); // hide existence for unauthorized

    db.Comments.Remove(comment);
    await db.SaveChangesAsync();
  }

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

  public async Task<IEnumerable<CommentTreeDto>> GetCommentTreeForPostId(int postId)
  {
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
    return tree;
  }
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
}