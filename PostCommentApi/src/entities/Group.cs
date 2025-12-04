public enum GroupRole
{
    Owner = 1,
    Admin = 2,
    Member = 3
}
public class Group
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<GroupMember> Members { get; set; } = new();
}
