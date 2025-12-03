using System.Text.Json;
using PostCommentApi.Exceptions;

namespace PostCommentApi.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
  public async Task Invoke(HttpContext context)
  {
    try
    {
      await next(context);
    }
    catch (UnauthorizedAccessException ua)
    {
      logger.LogWarning(ua, "Unauthorized: {Message}", ua.Message);
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;
      context.Response.ContentType = "application/json";
      var payload = JsonSerializer.Serialize(new { error = ua.Message });
      await context.Response.WriteAsync(payload);
    }
    catch (NotFoundException nf)
    {
      logger.LogWarning(nf, "Not found: {Message}", nf.Message);
      context.Response.StatusCode = StatusCodes.Status404NotFound;
      context.Response.ContentType = "application/json";
      var payload = JsonSerializer.Serialize(new { error = nf.Message });
      await context.Response.WriteAsync(payload);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unhandled exception");
      context.Response.StatusCode = StatusCodes.Status500InternalServerError;
      context.Response.ContentType = "application/json";
      var payload = JsonSerializer.Serialize(new { error = "An unexpected error occurred." });
      await context.Response.WriteAsync(payload);
    }
  }
}