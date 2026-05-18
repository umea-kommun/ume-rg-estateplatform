namespace Umea.se.Toolkit.Images;

public class ImageServiceOptions
{
    /// <summary>
    /// Required prefix for all cache keys, identifying the data source.
    /// Example: "myapp" results in paths like cache/myapp/images/123/original.webp
    /// </summary>
    public required string CacheKeyPrefix { get; set; }

    /// <summary>
    /// Default output format and quality for generated variants. Default: WebP, quality 80.
    /// </summary>
    public ImageOutputOptions DefaultOutput { get; set; } = new();

    /// <summary>
    /// L1 (in-memory) cache lifetime. Default: 24 hours.
    /// </summary>
    public TimeSpan MemoryCacheLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// L2 (blob storage) cache lifetime. Default: 180 days.
    /// </summary>
    public TimeSpan BlobCacheLifetime { get; set; } = TimeSpan.FromDays(180);
}
