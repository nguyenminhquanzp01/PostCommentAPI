using PostCommentApi.Dtos;
using PostCommentApi.Entities;
using PostCommentApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PostCommentApi.Services;

public class PageService(AppDb db) : IPageService
{
  public async Task<IEnumerable<PageDto>> GetPages()
  {
    var pages = await db.Pages
      .Include(p => p.Followers)
      .ToListAsync();
    return pages.Select(p => new PageDto
    {
      Id = p.Id,
      Name = p.Name,
      Description = p.Description,
      CreatedAt = p.CreatedAt,
      FollowersCount = p.Followers.Count
    });
  }
  public async Task<PageDto> GetPageById(int id)
  {
    var page = await db.Pages
      .Include(p => p.Followers)
      .FirstOrDefaultAsync(p => p.Id == id);
    if (page == null) throw new NotFoundException("Page", id);
    return new PageDto
    {
      Id = page.Id,
      Name = page.Name,
      Description = page.Description,
      CreatedAt = page.CreatedAt,
      FollowersCount = page.Followers.Count
    };
  }
  public async Task<PageDto> CreatePage(CreatePageDto dto, int userId)
  {
    var page = new Page
    {
      Name = dto.Name,
      Description = dto.Description
    };
    db.Pages.Add(page);
    await db.SaveChangesAsync();

    // Add creator as Owner
    var pageUser = new PageUser
    {
      PageId = page.Id,
      UserId = userId,
      Role = PageRole.Owner
    };
    db.PageUsers.Add(pageUser);
    await db.SaveChangesAsync();

    return await GetPageById(page.Id);
  }

  public async Task<PageDto> UpdatePage(int id, UpdatePageDto dto, int currentUserId, bool isAdmin)
  {
    var page = await db.Pages.FindAsync(id);
    if (page == null) throw new NotFoundException("Page", id);

    // Check if user is Owner or Admin of the page
    var userRole = await db.PageUsers
      .FirstOrDefaultAsync(pu => pu.PageId == id && pu.UserId == currentUserId);
    if (userRole == null || (userRole.Role != PageRole.Owner && userRole.Role != PageRole.Admin && !isAdmin))
      throw new UnauthorizedAccessException("You do not have permission to update this page.");

    page.Name = dto.Name;
    page.Description = dto.Description;
    await db.SaveChangesAsync();

    return await GetPageById(id);
  }

  public async Task DeletePage(int id, int currentUserId, bool isAdmin)
  {
    var page = await db.Pages.FindAsync(id);
    if (page == null) throw new NotFoundException("Page", id);

    // Check if user is Owner or Admin
    var userRole = await db.PageUsers
      .FirstOrDefaultAsync(pu => pu.PageId == id && pu.UserId == currentUserId);
    if (userRole == null || (userRole.Role != PageRole.Owner && userRole.Role != PageRole.Admin && !isAdmin))
      throw new UnauthorizedAccessException("You do not have permission to delete this page.");

    db.Pages.Remove(page);
    await db.SaveChangesAsync();
  }

  public async Task FollowPage(int pageId, int userId)
  {
    var page = await db.Pages.FindAsync(pageId);
    if (page == null) throw new NotFoundException("Page", pageId);

    var existing = await db.PageUsers
      .FirstOrDefaultAsync(pu => pu.PageId == pageId && pu.UserId == userId);
    if (existing != null) throw new ExistsException("User", userId);

    var pageUser = new PageUser
    {
      PageId = pageId,
      UserId = userId,
      Role = PageRole.Follower // Follow as Follower
    };
    db.PageUsers.Add(pageUser);
    await db.SaveChangesAsync();
  }

  public async Task<PostDto> CreatePostInPage(int pageId, CreatePostDto dto, int userId)
  {
    var page = await db.Pages.FindAsync(pageId);
    if (page == null) throw new NotFoundException("Page", pageId);

    // Check if user has permission to post in page (Editor, Admin, Owner)
    var userRole = await db.PageUsers
      .FirstOrDefaultAsync(pu => pu.PageId == pageId && pu.UserId == userId);
    if (userRole == null || (userRole.Role != PageRole.Owner && userRole.Role != PageRole.Admin && userRole.Role != PageRole.Editor))
      throw new UnauthorizedAccessException("You do not have permission to post in this page.");

    var post = new Post
    {
      Title = dto.Title,
      Content = dto.Content,
      UserId = userId,
      PageId = pageId
    };
    db.Posts.Add(post);
    await db.SaveChangesAsync();

    return new PostDto
    {
      Id = post.Id,
      Title = post.Title,
      Content = post.Content,
      CreatedAt = post.CreatedAt
    };
  }

  public async Task ChangeUserRole(int pageId, int targetUserId, PageRole newRole, int currentUserId, bool isAdmin)
  {
    var page = await db.Pages.FindAsync(pageId);
    if (page == null) throw new NotFoundException("Page", pageId);

    // Check if current user is Owner or Admin of the page
    var currentUserRole = await db.PageUsers
      .FirstOrDefaultAsync(pu => pu.PageId == pageId && pu.UserId == currentUserId);
    if (currentUserRole == null || (currentUserRole.Role != PageRole.Owner && currentUserRole.Role != PageRole.Admin && !isAdmin))
      throw new UnauthorizedAccessException("You do not have permission to change roles in this page.");

    var targetUserRole = await db.PageUsers
      .FirstOrDefaultAsync(pu => pu.PageId == pageId && pu.UserId == targetUserId);
    if (targetUserRole == null) throw new NotFoundException("User", targetUserId);

    // Prevent changing Owner's role unless global admin
    if (targetUserRole.Role == PageRole.Owner && !isAdmin)
      throw new UnauthorizedAccessException("Cannot change the Owner's role.");

    targetUserRole.Role = newRole;
    await db.SaveChangesAsync();
  }

  public async Task<IEnumerable<PostDto>> GetPostsForPage(int pageId)
  {
    var page = await db.Pages.FindAsync(pageId);
    if (page == null) throw new NotFoundException("Page", pageId);

    var posts = await db.Posts
      .Where(p => p.PageId == pageId)
      .Include(p => p.User)
      .OrderByDescending(p => p.CreatedAt)
      .ToListAsync();

    return posts.Select(p => new PostDto
    {
      Id = p.Id,
      Title = p.Title,
      Content = p.Content,
      CreatedAt = p.CreatedAt
    });
  }
}