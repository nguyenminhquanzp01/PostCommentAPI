namespace PostCommentApi.Dtos;

public class CreatePageDto
{
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
}