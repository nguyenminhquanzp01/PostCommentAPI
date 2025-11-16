public class CommentFlatDto
{
public int Id { get; set; }
public int? ParentId { get; set; }
public string Content { get; set; } = string.Empty;
public DateTime CreatedAt { get; set; }
}