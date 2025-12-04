using PostCommentApi.Entities;

public class GroupMember
{
    public int GroupId { get; set; }
    public int UserId { get; set; }

    public GroupRole Role { get; set; }

    public Group Group { get; set; }
    public User User { get; set; }
}
