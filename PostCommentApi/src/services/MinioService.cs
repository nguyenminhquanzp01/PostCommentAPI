using Amazon.S3;
using Amazon.S3.Model;
using System;

public class MinioService
{
  private readonly IAmazonS3 _s3Client;
  private readonly string _bucketName;

  public MinioService(IConfiguration config)
  {
    var settings = config.GetSection("MINIO");
    _bucketName = settings["BUCKET"]!;

    bool isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    var endpoint = isRunningInContainer ? "http://minio:9000" : settings["ENDPOINT"];

    _s3Client = new AmazonS3Client(
        settings["ROOT_USER"],
        settings["ROOT_PASSWORD"],
        new AmazonS3Config
        {
          ServiceURL = endpoint,
          ForcePathStyle = true // MUST for MinIO
        }
    );
  }

  public async Task<string> UploadAsync(IFormFile file, string keyPrefix = "")
  {
    await EnsureBucketExistsAsync();

    var key = $"{keyPrefix}/{Guid.NewGuid()}-{file.FileName}";

    using var stream = file.OpenReadStream();

    var request = new PutObjectRequest
    {
      BucketName = _bucketName,
      Key = key,
      InputStream = stream,
      ContentType = file.ContentType,
    };

    await _s3Client.PutObjectAsync(request);

    return key;
  }

  public async Task EnsureBucketExistsAsync()
  {
    try
    {
      var request = new ListBucketsRequest();
      var response = await _s3Client.ListBucketsAsync();
      if (!response.Buckets.Any(b => b.BucketName == _bucketName))
      {
        var putBucketRequest = new PutBucketRequest { BucketName = _bucketName };
        await _s3Client.PutBucketAsync(putBucketRequest);
      }
    }
    catch (Exception ex)
    {
      // Log or handle
      Console.WriteLine($"Error ensuring bucket exists: {ex.Message}");
    }
  }

  public string GetFileUrl(string key)
  {
    return $"{_s3Client.Config.ServiceURL}/{_bucketName}/{key}";
  }
}
