using PostCommentApi.Entities;

public enum FriendshipStatus
{
    Pending = 1,   // UserId gửi yêu cầu
    Accepted = 2,
    Blocked = 3
}
public class Friendship
{
    public int Id { get; set; }
    public int UserId { get; set; }        // người gửi lời mời
    public int FriendId { get; set; }      // người được mời
    
    public FriendshipStatus Status { get; set; }

    public User User { get; set; }
    public User Friend { get; set; }
}
