namespace PostCommentApi.Entities;

public class RefreshToken
{
  public int Id { get; set; }
  public string Token { get; set; } = string.Empty;
  public DateTime CreatedAtUtc { get; set; }
  public DateTime ExpiresAtUtc { get; set; }
  public DateTime? RevokedAtUtc { get; set; }
  // Optional: store which token replaced this one when rotating
  public string? ReplacedBy { get; set; }

  // Relationship
  public int UserId { get; set; }
  public User? User { get; set; }
}
