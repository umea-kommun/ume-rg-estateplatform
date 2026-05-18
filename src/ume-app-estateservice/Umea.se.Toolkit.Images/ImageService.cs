using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Umea.se.Toolkit.Images;

/// <summary>
/// Image processing service with FusionCache-based L1/L2 caching.
/// Provides automatic stampede protection, fail-safe, and eager refresh.
/// </summary>
public sealed class ImageService(IFusionCache cache, ImageServiceOptions options, ILogger<ImageService> logger) : IImageService
{
    private readonly IFusionCache _cache = cache;
    private readonly ImageServiceOptions _options = options;
    private readonly ILogger<ImageService> _logger = logger;
    private readonly FusionCacheEntryOptions _cacheOptions = CreateCacheOptions(options);

    /// <remarks>
    /// A cached variant can be served even when the original is no longer available, since variants
    /// are typically warmed by the consumer ahead of time. The caller may therefore observe a 200 for
    /// a resized request while a no-resize request returns 404 if the original was deleted out-of-band
    /// (manual delete, lifecycle policy). Re-warming is the consumer's responsibility. Validating
    /// original-blob existence per request is intentionally avoided to preserve the cache's value.
    /// </remarks>
    public async Task<ImageVariantBytes> GetVariantAsync(
        ImageVariantRequest request,
        Func<CancellationToken, Task<byte[]>> loadOriginal,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ImageId);
        ArgumentNullException.ThrowIfNull(loadOriginal);

        ImageOutputOptions outputOptions = ResolveOutputOptions(request);
        string thumbKey = ThumbnailKey(request, outputOptions);

        // Don't pass request cancellation token - let factory complete to populate cache
        // even if client disconnects. FusionCache timeouts protect against runaway operations.
        byte[] bytes = await _cache.GetOrSetAsync<byte[]>(
            thumbKey,
            async (ctx, token) =>
            {
                _logger.LogDebug("Resizing variant {Key}", thumbKey);
                byte[] originalBytes = await loadOriginal(token);
                ValidateNotEmpty(originalBytes, request.ImageId);
                ImageVariantBytes variant = ImageVariantEncoder.Resize(
                    originalBytes,
                    request.MaxWidth,
                    request.MaxHeight,
                    outputOptions);
                ctx.Options.SetSize(variant.Bytes.Length);
                return variant.Bytes;
            },
            _cacheOptions,
            CancellationToken.None);

        return new ImageVariantBytes(bytes, outputOptions.ContentType);
    }

    public async Task WarmVariantAsync(
        ImageVariantRequest request,
        Func<CancellationToken, Task<byte[]>> loadOriginal,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ImageId);
        ArgumentNullException.ThrowIfNull(loadOriginal);

        ImageOutputOptions outputOptions = ResolveOutputOptions(request);
        string thumbKey = ThumbnailKey(request, outputOptions);

        ZiggyCreatures.Caching.Fusion.MaybeValue<byte[]> existing =
            await _cache.TryGetAsync<byte[]>(thumbKey, _cacheOptions, ct);

        if (existing.HasValue)
        {
            return;
        }

        byte[] originalBytes = await loadOriginal(ct);
        ValidateNotEmpty(originalBytes, request.ImageId);
        ImageVariantBytes variant = ImageVariantEncoder.Resize(
            originalBytes,
            request.MaxWidth,
            request.MaxHeight,
            outputOptions);

        FusionCacheEntryOptions entryOptions = _cacheOptions.Duplicate();
        entryOptions.Size = variant.Bytes.Length;
        await _cache.SetAsync(thumbKey, variant.Bytes, entryOptions, tags: null, ct);
    }

    // Bump when the cached payload format changes so old L2 blobs are treated as a different
    // namespace (orphaned under the previous version, regenerated under the new one) instead of
    // failing to deserialize one-by-one on read.
    private const string CacheVersion = "v2";

    private string ThumbnailKey(ImageVariantRequest request, ImageOutputOptions outputOptions)
    {
        string path = request.ImageId.Replace(':', '/').Replace('\\', '/');
        return $"cache/{CacheVersion}/{_options.CacheKeyPrefix}/{path}/{request.MaxWidth ?? 0}x{request.MaxHeight ?? 0}.{outputOptions.Extension}";
    }

    private ImageOutputOptions ResolveOutputOptions(ImageVariantRequest request)
    {
        ImageOutputOptions outputOptions = request.Output ?? _options.DefaultOutput;
        outputOptions.Validate();
        return outputOptions;
    }

    private static FusionCacheEntryOptions CreateCacheOptions(ImageServiceOptions options) => new()
    {
        // Duration = logical freshness: controls when FusionCache considers an L2 entry stale and
        // re-runs the factory. Set to blob lifetime so L2 hits never trigger unnecessary re-fetches.
        // MemoryCacheDuration = actual L1 memory TTL, kept shorter to limit memory usage.
        Duration = options.BlobCacheLifetime,
        MemoryCacheDuration = options.MemoryCacheLifetime,
        DistributedCacheDuration = options.BlobCacheLifetime,
        // Default size estimate for L2 cache hits (100KB). Overridden with actual size in factory via adaptive caching.
        Size = 100 * 1024,
        Priority = CacheItemPriority.High,
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(365),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
        FactorySoftTimeout = TimeSpan.FromSeconds(5),
        FactoryHardTimeout = TimeSpan.FromSeconds(15),
        AllowBackgroundDistributedCacheOperations = true,  // Don't block caller while writing to L2 blob cache
    };

    private static void ValidateNotEmpty(byte[]? data, string imageId)
    {
        if (data is null || data.Length == 0)
        {
            throw new ImageNotFoundException($"Image not found: {imageId}");
        }
    }
}
