using PostCommentApi.Dtos;
using PostCommentApi.Entities;

namespace PostCommentApi.Services;

public interface IPageService
{
  public Task<IEnumerable<PageDto>> GetPages();
  public Task<PageDto> GetPageById(int id);
  public Task<PageDto> CreatePage(CreatePageDto dto, int userId);
  public Task<PageDto> UpdatePage(int id, UpdatePageDto dto, int currentUserId, bool isAdmin);
  public Task DeletePage(int id, int currentUserId, bool isAdmin);
  public Task FollowPage(int pageId, int userId);
  public Task<PostDto> CreatePostInPage(int pageId, CreatePostDto dto, int userId);
  public Task ChangeUserRole(int pageId, int targetUserId, PageRole newRole, int currentUserId, bool isAdmin);
  public Task<IEnumerable<PostDto>> GetPostsForPage(int pageId);
}