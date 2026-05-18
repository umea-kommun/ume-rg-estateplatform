using System.Globalization;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Umea.se.Toolkit.Images.Caching;

/// <summary>
/// IDistributedCache implementation backed by Azure Blob Storage.
/// Used as L2 cache for FusionCache, which only invokes the async members.
/// The synchronous interface members throw — call the async overloads instead.
/// </summary>
public sealed class BlobDistributedCache(BlobContainerClient container, ILogger<BlobDistributedCache> logger) : IDistributedCache
{
    private readonly BlobContainerClient _container = container;
    private readonly ILogger<BlobDistributedCache> _logger = logger;

    public byte[]? Get(string key) => throw new NotSupportedException("Use GetAsync.");

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        try
        {
            BlobClient blob = _container.GetBlobClient(key);
            Response<BlobDownloadResult> response = await blob.DownloadContentAsync(token);

            if (IsExpired(response.Value.Details.Metadata, key))
            {
                return null;
            }

            return response.Value.Content.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob cache get failed: {Key}", key);
            return null;
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw new NotSupportedException("Use SetAsync.");

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        try
        {
            BlobClient blob = _container.GetBlobClient(key);

            TimeSpan ttl = ResolveTtl(options);
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow + ttl;

            using MemoryStream stream = new(value);
            await blob.UploadAsync(stream, CreateUploadOptions(ttl, expiresAt), token);

            _logger.LogDebug("Blob cache set: {Key}, expires {ExpiresAt}", key, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob cache set failed: {Key}", key);
            // Don't throw - cache failures shouldn't break the application
        }
    }

    public void Remove(string key) => throw new NotSupportedException("Use RemoveAsync.");

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try
        {
            BlobClient blob = _container.GetBlobClient(key);
            await blob.DeleteIfExistsAsync(cancellationToken: token);
            _logger.LogDebug("Blob cache removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob cache remove failed: {Key}", key);
        }
    }

    // Blob storage doesn't support sliding expiration refresh; the no-op here matches
    // FusionCache's expectation that distributed Refresh is best-effort.
    public void Refresh(string key) { }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    private bool IsExpired(IDictionary<string, string> metadata, string key)
    {
        if (metadata.TryGetValue("expiresAt", out string? expiresAtStr)
            && DateTimeOffset.TryParse(expiresAtStr, null, DateTimeStyles.RoundtripKind, out DateTimeOffset expiresAt)
            && expiresAt < DateTimeOffset.UtcNow)
        {
            _logger.LogDebug("Blob cache entry expired: {Key}", key);
            return true;
        }

        return false;
    }

    private static TimeSpan ResolveTtl(DistributedCacheEntryOptions options)
        => options.AbsoluteExpirationRelativeToNow
            ?? options.SlidingExpiration
            ?? TimeSpan.FromHours(24);

    private static BlobUploadOptions CreateUploadOptions(TimeSpan ttl, DateTimeOffset expiresAt)
        => new()
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "application/octet-stream",
                CacheControl = $"public, max-age={(int)ttl.TotalSeconds}"
            },
            Metadata = new Dictionary<string, string>
            {
                ["expiresAt"] = expiresAt.ToString("O"),
                ["createdAt"] = DateTimeOffset.UtcNow.ToString("O")
            }
        };
}
