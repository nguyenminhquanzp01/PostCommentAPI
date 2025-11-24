public interface ICommentService
{
  public Task<IEnumerable<CommentDto>> GetCommentsForPostId(int postId);
  public Task<IEnumerable<CommentTreeDto>> GetCommentTreeForPostId(int postId);
  public Task<CommentDto> CreateCommentForPost(int postId, CreateCommentDto dto);
  public Task<CommentDto> UpdateComment(int commentId, UpdateCommentDto dto);
  public Task DeleteComment(int commentId);
}