using System.Text.Json;

public class ExceptionHandlingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<ExceptionHandlingMiddleware> _logger;

  public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task Invoke(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (NotFoundException nf)
    {
      _logger.LogWarning(nf, "Not found: {Message}", nf.Message);
      context.Response.StatusCode = StatusCodes.Status404NotFound;
      context.Response.ContentType = "application/json";
      var payload = JsonSerializer.Serialize(new { error = nf.Message });
      await context.Response.WriteAsync(payload);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unhandled exception");
      context.Response.StatusCode = StatusCodes.Status500InternalServerError;
      context.Response.ContentType = "application/json";
      var payload = JsonSerializer.Serialize(new { error = "An unexpected error occurred." });
      await context.Response.WriteAsync(payload);
    }
  }
}

