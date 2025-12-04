  namespace PostCommentApi.Entities;

  public class User
  {
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool IsAdmin { get; set; } = false;
    public List<Post> Posts { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
    // public List<UserRole> UserRoles { get; set; } = [];
    public List<Friendship> FriendShipAsUser { get; set; } = new();
    public List<Friendship> FriendShipAsFriend { get; set; } = new();
    public List<GroupMember> GroupMembers { get; set; } = new();
    public List<PageUser> Pages { get; set; } = new();
  }