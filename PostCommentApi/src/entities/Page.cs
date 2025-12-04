public enum PageRole
{
    Owner = 1,
    Admin = 2,
    Editor = 3,
    Follower = 4
}
public class Page
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PageUser> Followers { get; set; } = new();
}
