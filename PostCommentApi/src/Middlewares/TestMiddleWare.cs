public class TestMiddleWare(RequestDelegate next)
{
  public async Task Invoke(HttpContext context, TestService testService)
  {
    testService.Initialize("MiddlewareUser", 99);
    await next(context);
  }
}