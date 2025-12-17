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
    /// <summary>
    /// Upload an avatar image to the configured MinIO bucket and return the file key and public url.
    /// </summary>
    /// <param name="file">Multipart file uploaded by the client.</param>
    /// <returns>200 OK with fileKey and url on success; 400 BadRequest when file is missing.</returns>
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
