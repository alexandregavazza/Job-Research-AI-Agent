using Amazon.S3;
using Amazon.S3.Model;
using JobResearchAgent.Models;
using Microsoft.Extensions.Options;

namespace JobResearchAgent.Services.Storage;

public class S3DocumentStorage : IDocumentStorage
{
    private readonly IAmazonS3 _s3;
    private readonly ILogger<S3DocumentStorage> _logger;
    private readonly StorageOptions _options;

    public S3DocumentStorage(
        IAmazonS3 s3,
        ILogger<S3DocumentStorage> logger,
        IOptions<StorageOptions> options)
    {
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> StoreAsync(string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.S3Bucket))
        {
            _logger.LogWarning("Storage:S3Bucket not configured. Keeping local file: {FilePath}", filePath);
            return filePath;
        }

        var fileName = Path.GetFileName(filePath);
        var prefix = string.IsNullOrWhiteSpace(_options.S3Prefix)
            ? "documents"
            : _options.S3Prefix.Trim('/');
        var dateSegment = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var key = $"{prefix}/{dateSegment}/{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _options.S3Bucket,
            Key = key,
            FilePath = filePath
        };

        await _s3.PutObjectAsync(request, cancellationToken);

        return $"s3://{_options.S3Bucket}/{key}";
    }
}
