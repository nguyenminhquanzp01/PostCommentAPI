namespace PostCommentApi.Dtos;

public class CommentTreeDto
{
  public int Id { get; set; }
  public int? ParentId { get; set; }
  public string Content { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public List<CommentTreeDto> Replies { get; set; } = new();
}