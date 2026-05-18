using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umea.se.Toolkit.Images.Caching;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.ProtoBufNet;

namespace Umea.se.Toolkit.Images;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ImageService with FusionCache-based L1/L2 caching.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cacheKeyPrefix">Required prefix for all cache keys (e.g., "myapp").</param>
    /// <param name="configureOptions">Configure additional image service options.</param>
    /// <param name="configureBlobCache">Configure blob storage for L2 cache. If null or not configured, uses memory-only caching.</param>
    public static IServiceCollection AddImageService(
        this IServiceCollection services,
        string cacheKeyPrefix,
        Action<ImageServiceOptions>? configureOptions = null,
        Action<BlobCacheOptions>? configureBlobCache = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKeyPrefix);

        ImageServiceOptions imageOptions = new() { CacheKeyPrefix = cacheKeyPrefix };
        configureOptions?.Invoke(imageOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(imageOptions.CacheKeyPrefix);
        services.AddSingleton(imageOptions);

        BlobCacheOptions blobOptions = new();
        configureBlobCache?.Invoke(blobOptions);
        services.AddSingleton(blobOptions);

        RegisterCoreServices(services, blobOptions.IsConfigured);

        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services, bool useBlobCache)
    {
        if (useBlobCache)
        {
            // Shared BlobContainerClient: used by BlobDistributedCache (FusionCache L2) and made
            // available to consumers that store related payloads in the same container.
            services.AddSingleton<BlobContainerClient>(sp =>
            {
                BlobCacheOptions options = sp.GetRequiredService<BlobCacheOptions>();
                ILogger<BlobDistributedCache> logger = sp.GetRequiredService<ILogger<BlobDistributedCache>>();

                logger.LogInformation(
                    "BlobCacheOptions - UseConnectionString: {HasConnStr}, ServiceUri: {ServiceUri}, ContainerName: {Container}",
                    !string.IsNullOrWhiteSpace(options.ConnectionString),
                    options.ServiceUri,
                    options.ContainerName);

                return CreateBlobContainerClient(options, logger);
            });
        }

        // L2 Distributed Cache (Blob Storage or Memory fallback)
        services.AddSingleton<IDistributedCache>(sp =>
        {
            ILogger<BlobDistributedCache> logger = sp.GetRequiredService<ILogger<BlobDistributedCache>>();

            if (!useBlobCache)
            {
                logger.LogInformation("Falling back to memory-only distributed cache.");
                return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            }

            BlobContainerClient container = sp.GetRequiredService<BlobContainerClient>();
            return new BlobDistributedCache(container, logger);
        });

        // FusionCache with L1 memory + L2 Blob Storage + Protobuf serialization
        // Size is set via adaptive caching in factory, with a default fallback for L2 cache hits
        // No backplane needed: single-node deployment with immutable image content
        services.AddFusionCache()
            .WithOptions(o => o.EnableBestPracticesAdvisor = false)
            .WithMemoryCache(new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 500L * 1024 * 1024, // 500 MB
                CompactionPercentage = 0.25
            }))
            .WithSerializer(new FusionCacheProtoBufNetSerializer())
            .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>())
            .WithPostSetup((sp, cache) =>
            {
#if DEBUG
                ILogger cacheLogger = sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("FusionCache.ImageService");

                cache.Events.Miss += (_, e) => cacheLogger.LogDebug("Cache MISS: {Key}", e.Key);
                cache.Events.Set += (_, e) => cacheLogger.LogDebug("Cache SET: {Key}", e.Key);
                cache.Events.FailSafeActivate += (_, e) => cacheLogger.LogWarning("Cache FAILSAFE: {Key}", e.Key);
                cache.Events.Memory.Hit += (_, e) => cacheLogger.LogDebug(
                    "Cache HIT [L1-Memory{Stale}]: {Key}",
                    e.IsStale ? "-STALE" : "",
                    e.Key);
                cache.Events.Distributed.Hit += (_, e) => cacheLogger.LogDebug(
                    "Cache HIT [L2-Blob{Stale}]: {Key}",
                    e.IsStale ? "-STALE" : "",
                    e.Key);
#endif
            });

        services.TryAddSingleton<ImageService>();
        services.TryAddSingleton<IImageService>(sp => sp.GetRequiredService<ImageService>());
    }

    private static BlobContainerClient CreateBlobContainerClient(BlobCacheOptions options, ILogger logger)
    {
        BlobClientOptions clientOptions = new()
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                MaxRetries = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                MaxDelay = TimeSpan.FromSeconds(10)
            }
        };

        BlobServiceClient serviceClient;

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            logger.LogInformation("Using connection string for blob storage authentication");
            serviceClient = new BlobServiceClient(options.ConnectionString, clientOptions);
        }
        else if (options.ServiceUri is not null)
        {
            logger.LogInformation("Using DefaultAzureCredential for blob storage authentication: {Uri}", options.ServiceUri);
            serviceClient = new BlobServiceClient(options.ServiceUri, new DefaultAzureCredential(), clientOptions);
        }
        else
        {
            throw new InvalidOperationException("BlobCacheOptions requires either ConnectionString or ServiceUri to be configured.");
        }

        BlobContainerClient container = serviceClient.GetBlobContainerClient(options.ContainerName);

        if (options.CreateContainerIfNotExists)
        {
            container.CreateIfNotExists();
            logger.LogInformation("Ensured blob cache container {Container} exists", options.ContainerName);
        }

        logger.LogInformation("Blob distributed cache initialized: {Uri}", container.Uri);
        return container;
    }
}
