using PostCommentApi.Entities;

public class PageUser
{
    public int PageId { get; set; }
    public int UserId { get; set; }

    public PageRole Role { get; set; }

    public Page Page { get; set; }
    public User User { get; set; }
}
