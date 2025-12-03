using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly MinioService _minio;

    public UploadController(MinioService minio)
    {
        _minio = minio;
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        // Upload file
        var key = await _minio.UploadAsync(file, "avatars");

        // Public URL (MinIO local)
        var url = _minio.GetFileUrl(key);

        return Ok(new
        {
            fileKey = key,
            url = url
        });
    }
}
