public class Post
{
  public int Id { get; set; }
  public string Title { get; set; }
  public int UserId { get; set; }
  public string Content { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public User User { get; set; }
  public List<Comment> Comments { get; set; } = new();
}