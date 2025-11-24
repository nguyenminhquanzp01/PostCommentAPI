public class Comment
{
  public int Id { get; set; }
  public string Content { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public int UserId { get; set; }
  public User? User { get; set; }
  public int PostId { get; set; }
  public Post? Post { get; set; }
  public int? ParentId { get; set; }
  public Comment? Parent { get; set; }
  public List<Comment> Replies { get; set; } = new();
}