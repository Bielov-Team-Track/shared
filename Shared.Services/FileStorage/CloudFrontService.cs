using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Services.FileStorage.Intefaces;

namespace Shared.Services.FileStorage;

public class CloudFrontService : ICloudFrontService
{
    private readonly IAmazonCloudFront? _cloudFrontClient;
    private readonly string? _distributionId;
    private readonly ILogger<CloudFrontService> _logger;

    public CloudFrontService(
        IOptions<S3Settings> s3Settings,
        ILogger<CloudFrontService> logger,
        IAmazonCloudFront? cloudFrontClient = null)
    {
        _distributionId = s3Settings.Value.CloudFrontDistributionId;
        _logger = logger;
        _cloudFrontClient = cloudFrontClient;
    }

    public async Task InvalidateAsync(params string[] paths)
    {
        if (string.IsNullOrEmpty(_distributionId) || _cloudFrontClient == null || paths.Length == 0)
            return;

        var normalizedPaths = paths
            .Select(p => p.StartsWith('/') ? p : $"/{p}")
            .ToList();

        try
        {
            var request = new CreateInvalidationRequest
            {
                DistributionId = _distributionId,
                InvalidationBatch = new InvalidationBatch
                {
                    CallerReference = $"spike-{DateTime.UtcNow.Ticks}",
                    Paths = new Paths
                    {
                        Quantity = normalizedPaths.Count,
                        Items = normalizedPaths
                    }
                }
            };

            await _cloudFrontClient.CreateInvalidationAsync(request);
            _logger.LogInformation("CloudFront invalidation created for {PathCount} paths", normalizedPaths.Count);
        }
        catch (Exception ex)
        {
            // Log but don't fail — invalidation is best-effort, cached content expires naturally
            _logger.LogWarning(ex, "Failed to create CloudFront invalidation for paths: {Paths}",
                string.Join(", ", normalizedPaths));
        }
    }
}
