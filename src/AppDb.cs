using Microsoft.EntityFrameworkCore;

public class AppDb : DbContext
{
  public AppDb(DbContextOptions options) : base(options) { }


  public DbSet<User> Users => Set<User>();
  public DbSet<Post> Posts => Set<Post>();
  public DbSet<Comment> Comments => Set<Comment>();


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
    modelBuilder.Entity<User>().ToTable("User");
    modelBuilder.Entity<Post>().Property(p => p.Title).HasMaxLength(500);
    modelBuilder.Entity<Post>().ToTable("Post");
    modelBuilder.Entity<Comment>().Property(c => c.Content).HasColumnType("longtext");
    modelBuilder.Entity<Comment>().ToTable("Comment");
  }
}