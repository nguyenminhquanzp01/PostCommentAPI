using PostCommentApi.Dtos;

namespace PostCommentApi.Services;

public interface IPostService
{
  public Task<IEnumerable<PostDto>> GetNextPostsFromPostId(int lastPostId);
  public Task<PostDto> GetPostById(int id);
  public Task<PostDto> CreatePost(CreatePostDto dto, int userId);
  // currentUserId: id of the caller (from token)
  // isAdmin: whether the caller has admin privileges
  public Task<PostDto> UpdatePost(int id, CreatePostDto dto, int currentUserId, bool isAdmin);
  public Task<IEnumerable<PostDto>> GetNextPostsForUser(int lastId, int userId);
  public Task DeletePost(int id, int currentUserId, bool isAdmin);
  public Task<IEnumerable<PostDto>> FilterPosts(PostQueryDto query);
}