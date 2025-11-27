using PostCommentApi.Entities;

namespace PostCommentApi.Utilities;

public static class Seeder
{
  public static async Task SeedUserPostComment(AppDb context)
  {
    if (context.Users.Any())
      return;

    // Seed 10 users total (single batch)
    const int totalUsers = 100;
    const int batch = 20;

    for (int i = 0; i < totalUsers; i += batch)
    {
      var users = new List<User>();
      var posts = new List<Post>();
      var comments = new List<Comment>();

      for (int j = 0; j < batch; j++)
      {
        var globalIndex = i + j;
        var user = new User
        {
          UserName = $"user{globalIndex}",
          Password = "password",
          Name = RandomHelper.RandomString(8),
          Email = RandomHelper.RandomString(6) + "@mail.com"
        };

        users.Add(user);

        int postCount = RandomHelper.RandomInt(3, 5);

        for (int p = 0; p < postCount; p++)
        {
          var post = new Post
          {
            Title = RandomHelper.RandomString(15),
            Content = RandomHelper.RandomString(60),
            User = user
          };

          posts.Add(post);

          // Root comments (level 0)
          int rootCommentCount = RandomHelper.RandomInt(10, 20);
          var rootComments = new List<Comment>();

          for (int c = 0; c < rootCommentCount; c++)
          {
            var root = new Comment
            {
              Content = RandomHelper.RandomString(20),
              Post = post,
              Parent = null,
              User = user
            };
            comments.Add(root);
            rootComments.Add(root);
          }

          // Level 1 replies (0-2 per root)
          foreach (var root in rootComments)
          {
            int lvl1Count = RandomHelper.RandomInt(0, 2);
            var lvl1Comments = new List<Comment>();

            for (int l1 = 0; l1 < lvl1Count; l1++)
            {
              var c1 = new Comment
              {
                Content = RandomHelper.RandomString(20),
                Post = post,
                Parent = root,
                User = user
              };
              comments.Add(c1);
              lvl1Comments.Add(c1);
            }

            // Level 2 replies (0-1 per level1)
            foreach (var l1c in lvl1Comments)
            {
              int lvl2Count = RandomHelper.RandomInt(0, 1);
              for (int l2 = 0; l2 < lvl2Count; l2++)
              {
                comments.Add(new Comment
                {
                  Content = RandomHelper.RandomString(20),
                  Post = post,
                  Parent = l1c,
                  User = user
                });
              }
            }
          }
        }
      }

      await context.Users.AddRangeAsync(users);
      await context.Posts.AddRangeAsync(posts);
      await context.Comments.AddRangeAsync(comments);

      await context.SaveChangesAsync();

      Console.WriteLine($"Seeded {i + batch} users...");
    }
  }

}