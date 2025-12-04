namespace PostCommentApi.Dtos;

public class PageDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
  public DateTime CreatedAt { get; set; }
  public int FollowersCount { get; set; }
}