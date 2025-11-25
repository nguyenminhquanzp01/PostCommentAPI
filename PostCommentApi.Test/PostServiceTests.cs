using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class PostServiceTests
{
  [Fact]
  public async Task GetNextPostsFromId_WhenLastIdIsMaxValue_ReturnsLatestPosts()
  {
    // Arrange - create in-memory db
    var options = new DbContextOptionsBuilder<AppDb>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    // seed data
    using (var db = new AppDb(options))
    {
      var user = new User { UserName = "tester", Name = "Tester", Email = "t@mail.com", Password = "secret" };
      db.Users.Add(user);

      var now = DateTime.UtcNow;
      // create 3 posts with different CreatedAt
      db.Posts.Add(new Post { User = user, Title = "P1", Content = "c1", CreatedAt = now.AddMinutes(-3) });
      db.Posts.Add(new Post { User = user, Title = "P2", Content = "c2", CreatedAt = now.AddMinutes(-2) });
      db.Posts.Add(new Post { User = user, Title = "P3", Content = "c3", CreatedAt = now.AddMinutes(-1) });

      await db.SaveChangesAsync();
    }

    // Act
    using (var db = new AppDb(options))
    {
      var cfg = new MapperConfiguration(cfg => cfg.AddProfile(new PostProfile()));
      var mapper = cfg.CreateMapper();
      var svc = new PostService(db, mapper);

      var results = (await svc.GetNextPostsFromId(int.MaxValue)).ToList();

      // Assert
      results.Should().HaveCount(3);
      results[0].Title.Should().Be("P3");
      results[1].Title.Should().Be("P2");
      results[2].Title.Should().Be("P1");
    }
  }
}
