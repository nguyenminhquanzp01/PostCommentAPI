namespace PostCommentApi.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } // "Admin", "User"
    public List<UserRole> UserRoles { get; set; }
}