using Microsoft.EntityFrameworkCore;
using PostCommentApi.Entities;

namespace PostCommentApi;

public class AppDb(DbContextOptions options) : DbContext(options)
{
  public DbSet<User> Users => Set<User>();
  public DbSet<Post> Posts => Set<Post>();
  public DbSet<Comment> Comments => Set<Comment>();
  // public DbSet<UserRole> UserRoles => Set<UserRole>();
  // public DbSet<Role> Roles => Set<Role>();
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Comment>()
      .HasOne(c => c.Parent)
      .WithMany(c => c.Replies)
      .HasForeignKey(c => c.ParentId)
      .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<User>(
      u =>
      {
        u.Property(u => u.Email).HasMaxLength(200);
        u.Property(u => u.Name).HasMaxLength(200);
      }
    );
    modelBuilder.Entity<User>().ToTable("user");
    modelBuilder.Entity<Post>().Property(p => p.Title).HasMaxLength(500);
    modelBuilder.Entity<Post>().ToTable("post");
    modelBuilder.Entity<Comment>().Property(c => c.Content).HasColumnType("longtext");
    modelBuilder.Entity<Comment>().ToTable("comment");
    // modelBuilder.Entity<UserRole>().ToTable("user_role");
    // modelBuilder.Entity<Role>().ToTable("role");
  }
}