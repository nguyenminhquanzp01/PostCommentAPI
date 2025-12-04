using Microsoft.EntityFrameworkCore;
using PostCommentApi.Entities;

namespace PostCommentApi;

public class AppDb(DbContextOptions options) : DbContext(options)
{
  public DbSet<User> Users => Set<User>();
  public DbSet<Post> Posts => Set<Post>();
  public DbSet<Comment> Comments => Set<Comment>();
  public DbSet<Friendship> Friendships => Set<Friendship>();
  public DbSet<Group> Groups => Set<Group>();
  public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
  public DbSet<Page> Pages => Set<Page>();
  public DbSet<PageUser> PageUsers => Set<PageUser>();
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Comment>()
      .HasOne(c => c.Parent)
      .WithMany(c => c.Replies)
      .HasForeignKey(c => c.ParentId)
      .OnDelete(DeleteBehavior.Cascade);
    modelBuilder.Entity<User>().HasMany(e => e.FriendShipAsUser).WithOne(f => f.User).HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
    modelBuilder.Entity<User>().HasMany(e => e.FriendShipAsFriend).WithOne(f => f.Friend).HasForeignKey(f => f.FriendId).OnDelete(DeleteBehavior.Cascade); 
    modelBuilder.Entity<User>(
      u =>
      {
        u.Property(u => u.Email).HasMaxLength(200);
        u.Property(u => u.Name).HasMaxLength(200);
      }
    );
    modelBuilder.Entity<GroupMember>()
      .HasKey(gm => new { gm.GroupId, gm.UserId });
    modelBuilder.Entity<PageUser>()
      .HasKey(pu => new { pu.PageId, pu.UserId });
    modelBuilder.Entity<User>().ToTable("user");
    modelBuilder.Entity<Post>().Property(p => p.Title).HasMaxLength(500);
    modelBuilder.Entity<Post>().ToTable("post");
    modelBuilder.Entity<Comment>().Property(c => c.Content).HasColumnType("longtext");
    modelBuilder.Entity<Comment>().ToTable("comment");
  }
}