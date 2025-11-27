using PostCommentApi.Dtos;

namespace PostCommentApi.Services;

public interface IPostService
{
  public Task<IEnumerable<PostDto>> GetNextPostsFromPostId(int lastPostId);
  public Task<PostDto> GetPostById(int id);
  public Task<PostDto> CreatePost(CreatePostDto dto, int userId);
  public Task<PostDto> UpdatePost(int id, CreatePostDto dto);
  public Task<PostDto> CreatePostForUserName(CreatePostDto dto, string username);
  public Task<IEnumerable<PostDto>> GetNextPostsForUserName(int lastId, string username);
  public Task DeletePost(int id);
  public Task<IEnumerable<PostDto>> FilterPosts(PostQueryDto query);
  public Task<bool> DoUserHasPost(string username, int postId);
}