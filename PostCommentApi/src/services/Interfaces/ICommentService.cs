using PostCommentApi.Dtos;

namespace PostCommentApi.Services;

public interface ICommentService
{
  public Task<IEnumerable<CommentDto>> GetCommentsForPostId(int postId);
  public Task<IEnumerable<CommentTreeDto>> GetCommentTreeForPostId(int postId);
  // currentUserId: caller's user id (from token); isAdmin: caller admin flag
  // If caller is not admin, service will set the comment's author to currentUserId.
  public Task<CommentDto> CreateCommentForPost(int postId, CreateCommentDto dto, int currentUserId, bool isAdmin);
  public Task<CommentDto> UpdateComment(int commentId, UpdateCommentDto dto, int currentUserId, bool isAdmin);
  public Task DeleteComment(int commentId, int currentUserId, bool isAdmin);
}