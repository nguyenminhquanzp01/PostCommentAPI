using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PostCommentApi.Middlewares;

public class PostOwnershipMiddleware(RequestDelegate next, ILogger<PostOwnershipMiddleware> logger)
{
  public async Task Invoke(HttpContext context)
  {
    // Try to get route values (requires UseRouting() run before this middleware)
    var routeValues = context.Request.RouteValues;
    if (routeValues == null || !routeValues.ContainsKey("username") || !routeValues.ContainsKey("postId"))
    {
      await next(context);
      return;
    }

    var username = routeValues["username"]?.ToString();
    var postIdRaw = routeValues["postId"]?.ToString();
    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(postIdRaw))
    {
      await next(context);
      return;
    }

    if (!int.TryParse(postIdRaw, out var postId))
    {
      // invalid postId in route -> continue, controller will likely handle
      await next(context);
      return;
    }

    try
    {
      // Resolve AppDb from DI
      var db = context.RequestServices.GetService(typeof(AppDb)) as AppDb;
      if (db == null)
      {
        logger.LogWarning("AppDb not available in request services");
        await next(context);
        return;
      }

      var post = await db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId);
      if (post == null)
      {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = $"Post {postId} not found" }));
        return;
      }

      var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == post.UserId);
      if (user == null || !string.Equals(user.UserName, username, StringComparison.OrdinalIgnoreCase))
      {
        // username does not own the post -> 404 to avoid exposing existence
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Post not found for this user" }));
        return;
      }

      // owner matches, continue
      await next(context);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error in PostOwnershipMiddleware");
      context.Response.StatusCode = StatusCodes.Status500InternalServerError;
      context.Response.ContentType = "application/json";
      await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal server error" }));
    }
  }
}