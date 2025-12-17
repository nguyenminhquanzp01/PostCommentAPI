using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PostCommentApi.Dtos;
using PostCommentApi.Entities;
using PostCommentApi.Exceptions;
using StackExchange.Redis;

namespace PostCommentApi.Services;

public class PostService(
    AppDb db,
    IMapper mapper,
    IDatabase redis,
    ILogger<PostService> logger

  ) : IPostService
{
  public async Task<PostDto> CreatePost(CreatePostDto dto, int userId)
  {
    var user = await db.Users.FindAsync(userId);
    if (user == null) throw new NotFoundException("User", userId);
    var post = mapper.Map<Post>(dto);
    post.UserId = userId;
    db.Posts.Add(post);
    await db.SaveChangesAsync();
    await redis.KeyDeleteAsync("post:latest");
    return mapper.Map<PostDto>(post);
  }

  public async Task DeletePost(int id, int currentUserId, bool isAdmin)
  {
    var post = await db.Posts.FindAsync(id);
    if (post == null) throw new NotFoundException("Post", id);

    // Only admin or owner can delete
    if (!isAdmin && post.UserId != currentUserId)
      throw new NotFoundException("Post", id); // hide existence when not owner

    // Remove comments for the post first to avoid FK issues if cascade isn't configured.
    var postComments = db.Comments.Where(c => c.PostId == id);
    db.Comments.RemoveRange(postComments);
    await redis.KeyDeleteAsync($"post:{id}");
    await redis.KeyDeleteAsync("post:latest");
    db.Posts.Remove(post);
    await db.SaveChangesAsync();
  }


  public async Task<IEnumerable<PostDto>> FilterPosts(PostQueryDto query)
  {
    var q = db.Posts.AsQueryable();

    if (!string.IsNullOrEmpty(query.Keyword))
    {
      q = q.Where(p =>
        p.Title.Contains(query.Keyword) ||
        p.Content.Contains(query.Keyword));
    }

    if (query.FromDate.HasValue)
      q = q.Where(p => p.CreatedAt >= query.FromDate.Value);

    if (query.ToDate.HasValue)
      q = q.Where(p => p.CreatedAt <= query.ToDate.Value);

    // Sorting
    if (!string.IsNullOrEmpty(query.Sort))
    {
      q = query.Sort switch
      {
        "createdAt" => q.OrderBy(p => p.CreatedAt),
        "-createdAt" => q.OrderByDescending(p => p.CreatedAt),
        "title" => q.OrderBy(p => p.Title),
        "-title" => q.OrderByDescending(p => p.Title),
        _ => q.OrderByDescending(p => p.CreatedAt) // default
      };
    }
    else
    {
      q = q.OrderByDescending(p => p.CreatedAt);
    }

    // Pagination
    q = q.Skip(query.Offset).Take(query.Limit);

    return await q
      .Select(p => mapper.Map<PostDto>(p))
      .ToListAsync();
  }
  public async Task<IEnumerable<PostDto>> GetPriviousPostsFromPostIdForUser(int lastId, int userId)
  {
    var user = await db.Users.FindAsync(userId);
    if (user == null) throw new NotFoundException("User", userId);

    const int pageSize = 10;

    if (lastId == int.MaxValue)
    {
      var latest = await db.Posts
        .Where(p => p.UserId == user.Id)
        .OrderByDescending(p => p.CreatedAt)
        .ThenByDescending(p => p.Id)
        .Take(pageSize)
        .Select(p => mapper.Map<PostDto>(p))
        .ToListAsync();

      return latest;
    }

    var last = await db.Posts.FindAsync(lastId);
    if (last == null || last.UserId != user.Id) throw new NotFoundException("Post", lastId);

    var posts = await db.Posts
      .Where(p => p.UserId == user.Id && (p.CreatedAt < last.CreatedAt || (p.CreatedAt == last.CreatedAt && p.Id < last.Id)))
      .OrderByDescending(p => p.CreatedAt)
      .ThenByDescending(p => p.Id)
      .Take(pageSize)
      .Select(p => mapper.Map<PostDto>(p))
      .ToListAsync();

    return posts;
  }

  /// <summary>
  /// sắp xếp tất cả post theo thời gian, lấy 10 bài post cũ hơn bài post có id = lastId
  /// </summary>
  /// <param name="lastPostId"></param>
  /// <returns></returns>
  /// <exception cref="NotFoundException"></exception>
  public async Task<IEnumerable<PostDto>> GetPreviousPostsFromPostId(int lastPostId)
  {
    const int pageSize = 10;
    //Nếu lastId = int.MaxValue thì lấy 10 bài post mới nhất 
    if (lastPostId == int.MaxValue)
    {
      var cacheKey = "post:latest";
      var cache = await redis.StringGetAsync(cacheKey);
      if (cache.HasValue)
      {
        var cachedPosts = JsonSerializer.Deserialize<List<PostDto>>(cache);
        if (cachedPosts != null)
        {
          logger.LogInformation("Cache hit for latest posts .");
          return cachedPosts;
        }
      }

      var latest = await db.Posts
        .OrderByDescending(p => p.CreatedAt)
        .ThenByDescending(p => p.Id)
        .Take(pageSize)
        .Select(p => mapper.Map<PostDto>(p))
        .ToListAsync();
      var serializedPosts = JsonSerializer.Serialize(latest);
      await redis.StringSetAsync(cacheKey, serializedPosts, TimeSpan.FromMinutes(5));
      return latest;
    }
    //Nếu không thì lấy 10 bài post cũ hơn bài post có id = lastId
    var last = await db.Posts.FindAsync(lastPostId);
    if (last == null) throw new NotFoundException("Post", lastPostId);
    var posts = await db.Posts
      .Where(p => p.CreatedAt < last.CreatedAt || (p.CreatedAt == last.CreatedAt && p.Id < last.Id))
      .OrderByDescending(p => p.CreatedAt)
      .ThenByDescending(p => p.Id)
      .Take(pageSize)
      .Select(p => mapper.Map<PostDto>(p))
      .ToListAsync();

    return posts;
  }

  public async Task<PostDto> GetPostById(int id)
  {
    var cacheKey = $"post:{id}";
    var cache = await redis.StringGetAsync(cacheKey);
    if (cache.HasValue)
    {
      if (JsonSerializer.Deserialize<PostDto>(cache!) != null)
        return JsonSerializer.Deserialize<PostDto>(cache!)!;
    }

    var post = await db.Posts.FindAsync(id);
    if (post == null) throw new NotFoundException("Post", id);

    var dto = mapper.Map<PostDto>(post);
    await redis.StringSetAsync(cacheKey,
        JsonSerializer.Serialize(dto),
        TimeSpan.FromMinutes(10));

    return dto;

  }
  public async Task<PostDto> UpdatePost(int id, CreatePostDto dto, int currentUserId, bool isAdmin)
  {
    var post = await db.Posts.FindAsync(id);
    if (post == null) throw new NotFoundException("Post", id);

    // Only admin or owner can update
    if (!isAdmin && post.UserId != currentUserId)
      throw new NotFoundException("Post", id); // do not reveal existence to unauthorized users

    // Update allowed fields
    post.Title = dto.Title;
    post.Content = dto.Content;

    await db.SaveChangesAsync();
    await redis.KeyDeleteAsync($"post:{id}");
    await redis.KeyDeleteAsync("post:latest");
    return mapper.Map<PostDto>(post);
  }
}