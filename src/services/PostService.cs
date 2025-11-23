
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class PostService : IPostService
{
  private readonly AppDb _db;
  private readonly IMapper _mapper;
  public PostService(AppDb db, IMapper mapper)
  {
    _db = db;
    _mapper = mapper;
  }
  public async Task<PostDto> CreatePost(CreatePostDto dto, int userId)
  {
    var user = await _db.Users.FindAsync(userId);
    if (user == null) throw new NotFoundException("User", userId);
    var post = _mapper.Map<Post>(dto);
    post.UserId = userId;
    _db.Posts.Add(post);
    await _db.SaveChangesAsync();
    return _mapper.Map<PostDto>(post);
  }
  public async Task<PostDto> CreatePostForUserName(CreatePostDto dto, string username)
  {
    var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);
    if (user == null) throw new NotFoundException("User", username);

    var post = _mapper.Map<Post>(dto);
    post.UserId = user.Id;
    post.CreatedAt = DateTime.UtcNow;
    _db.Posts.Add(post);
    await _db.SaveChangesAsync();
    return _mapper.Map<PostDto>(post);
  }

  public async Task DeletePost(int id)
  {
    var post = await _db.Posts.FindAsync(id);
    if (post == null) throw new NotFoundException("Post", id);

    // Remove comments for the post first to avoid FK issues if cascade isn't configured.
    var postComments = _db.Comments.Where(c => c.PostId == id);
    _db.Comments.RemoveRange(postComments);

    _db.Posts.Remove(post);
    await _db.SaveChangesAsync();
  }

  public async Task<IEnumerable<PostDto>> GetNextPostsForUserName(int lastId, string username)
  {
    var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);
    if (user == null) throw new NotFoundException("User", username);

    const int pageSize = 10;

    if (lastId == int.MaxValue)
    {
      var latest = await _db.Posts
        .Where(p => p.UserId == user.Id)
        .OrderByDescending(p => p.CreatedAt)
        .ThenByDescending(p => p.Id)
        .Take(pageSize)
        .Select(p => _mapper.Map<PostDto>(p))
        .ToListAsync();

      return latest;
    }

    var last = await _db.Posts.FindAsync(lastId);
    if (last == null || last.UserId != user.Id) throw new NotFoundException("Post", lastId);

    var posts = await _db.Posts
      .Where(p => p.UserId == user.Id && (p.CreatedAt < last.CreatedAt || (p.CreatedAt == last.CreatedAt && p.Id < last.Id)))
      .OrderByDescending(p => p.CreatedAt)
      .ThenByDescending(p => p.Id)
      .Take(pageSize)
      .Select(p => _mapper.Map<PostDto>(p))
      .ToListAsync();

    return posts;
  }

  public async Task<IEnumerable<PostDto>> GetNextPostsFromId(int lastId)
  {
    const int pageSize = 10;
    if (lastId == int.MaxValue)
    {
      var latest = await _db.Posts
        .OrderByDescending(p => p.CreatedAt)
        .ThenByDescending(p => p.Id)
        .Take(pageSize)
        .Select(p => _mapper.Map<PostDto>(p))
        .ToListAsync();

      return latest;
    }
    var last = await _db.Posts.FindAsync(lastId);
    if (last == null) throw new NotFoundException("Post", lastId);
    var posts = await _db.Posts
      .Where(p => p.CreatedAt < last.CreatedAt || (p.CreatedAt == last.CreatedAt && p.Id < last.Id))
      .OrderByDescending(p => p.CreatedAt)
      .ThenByDescending(p => p.Id)
      .Take(pageSize)
      .Select(p => _mapper.Map<PostDto>(p))
      .ToListAsync();

    return posts;
  }

  public async Task<PostDto> GetPostById(int id)
  {
    var post = await _db.Posts.FindAsync(id);
    if (post == null) throw new NotFoundException("Post", id);
    return _mapper.Map<PostDto>(post);
  }

  public async Task<PostDto> UpdatePost(int id, CreatePostDto dto)
  {
    var post = await _db.Posts.FindAsync(id);
    if (post == null) throw new NotFoundException("Post", id);

    // Update allowed fields
    post.Title = dto.Title;
    post.Content = dto.Content;

    await _db.SaveChangesAsync();
    return _mapper.Map<PostDto>(post);
  }
}