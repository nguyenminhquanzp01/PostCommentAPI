using Microsoft.AspNetCore.Mvc;

public class TestController(TestService testService): ControllerBase
{
  [HttpGet("/test")]
  public IActionResult GetTestInfo()
  {
    return Ok(testService);
  }
}