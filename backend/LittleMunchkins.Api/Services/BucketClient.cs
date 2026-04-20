using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace LittleMunchkins.Api.Services;

public class BucketClient
{
    private readonly AmazonS3Client _s3;
    private readonly string _bucket;

    public BucketClient(IConfiguration config)
    {
        var endpoint = config["BUCKET_ENDPOINT"] ?? throw new Exception("BUCKET_ENDPOINT not set");
        var key = config["BUCKET_ACCESS_KEY_ID"] ?? throw new Exception("BUCKET_ACCESS_KEY_ID not set");
        var secret = config["BUCKET_SECRET_ACCESS_KEY"] ?? throw new Exception("BUCKET_SECRET_ACCESS_KEY not set");
        _bucket = config["BUCKET_NAME"] ?? throw new Exception("BUCKET_NAME not set");

        _s3 = new AmazonS3Client(
            new BasicAWSCredentials(key, secret),
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
            });
    }

    public async Task<string> GenerateUploadUrlAsync(string objectKey, string contentType, int expiryMinutes = 15)
    {
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
        };
        return await _s3.GetPreSignedURLAsync(req);
    }

    public async Task<string> GenerateDownloadUrlAsync(string objectKey, int expiryMinutes = 60)
    {
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
        };
        return await _s3.GetPreSignedURLAsync(req);
    }

    public async Task<Stream> GetObjectStreamAsync(string objectKey)
    {
        var resp = await _s3.GetObjectAsync(_bucket, objectKey);
        return resp.ResponseStream;
    }
}
