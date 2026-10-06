using System.Globalization;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.Toolkit.Images.Caching;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.ProtoBufNet;

namespace Umea.se.EstateService.Test.Caching;

public class BlobDistributedCacheTests
{
    [Fact]
    public async Task SetAsync_ViaFusionCacheWithFailSafe_StoresFailSafeMaxDurationExpiry()
    {
        RecordingContainerClient container = new();
        using FusionCache fusionCache = new(new FusionCacheOptions());
        fusionCache.SetupDistributedCache(
            new BlobDistributedCache(container, NullLogger<BlobDistributedCache>.Instance),
            new FusionCacheProtoBufNetSerializer());

        await fusionCache.SetAsync("key", new byte[] { 1, 2, 3 }, new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromDays(30),
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromDays(365),
        });

        container.Blob.StoredExpiry().ShouldBe(DateTimeOffset.UtcNow.AddDays(365), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SetAsync_WithRelativeExpiration_StoresExpiryFromNow()
    {
        RecordingContainerClient container = new();
        BlobDistributedCache cache = new(container, NullLogger<BlobDistributedCache>.Instance);

        await cache.SetAsync("key", [1, 2, 3], new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30),
        });

        container.Blob.StoredExpiry().ShouldBe(DateTimeOffset.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
    }

    private sealed class RecordingContainerClient : BlobContainerClient
    {
        public RecordingBlobClient Blob { get; } = new();

        public override BlobClient GetBlobClient(string blobName) => Blob;
    }

    private sealed class RecordingBlobClient : BlobClient
    {
        private BlobUploadOptions? _uploadOptions;

        public override Task<Response<BlobContentInfo>> UploadAsync(Stream content, BlobUploadOptions options, CancellationToken cancellationToken = default)
        {
            _uploadOptions = options;
            return Task.FromResult<Response<BlobContentInfo>>(null!);
        }

        public DateTimeOffset StoredExpiry()
            => DateTimeOffset.Parse(_uploadOptions.ShouldNotBeNull().Metadata["expiresAt"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
