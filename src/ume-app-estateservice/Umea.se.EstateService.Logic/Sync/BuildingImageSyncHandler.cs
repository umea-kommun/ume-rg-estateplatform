using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.ServiceAccess.Images;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Logic.Sync;

/// <summary>
/// Sync step that ensures every advertised building image has a normalized
/// original blob and a warm 300-wide variant in FusionCache. Mutates each
/// <see cref="BuildingEntity.ImageIds"/> in place to contain only successfully-written IDs.
/// </summary>
public sealed class BuildingImageSyncHandler(
    IPythagorasClient pythagorasClient,
    IOriginalImageStore originalStore,
    IImageService imageService,
    IDataStore dataStore,
    ILogger<BuildingImageSyncHandler> logger) : IBuildingImageSyncHandler
{
    private const int ThumbnailWidth = 300;
    private const int MaxConcurrency = 3;
    private const int MaxOriginalDimension = 2560;
    private const int OriginalWebPQuality = 95;

    // Promote the per-sync summary to Warning when more than this fraction of discovered images
    // were excluded. Below the threshold we stay at Information so a healthy run is quiet.
    private const double DegradedExclusionRatio = 0.10;
    private const int SampleIdLimit = 5;

    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        // Plan invariant: /images never advertises IDs without an original. A blob outage during
        // sync drops images until the next successful sync; aborting here keeps that contract
        // honest instead of marking every image as excluded against a phantom empty set.
        IReadOnlySet<int> existingIds;
        try
        {
            existingIds = await originalStore.ListExistingImageIdsAsync(cancellationToken);
        }
        catch (ExternalServiceUnavailableException ex)
        {
            logger.LogError(ex, "Image sync aborted: could not list existing originals.");
            return;
        }

        // Distinct image IDs across all buildings — same image referenced by multiple buildings is
        // still one blob, so process it once. Survival is a property of the image, not the reference.
        int[] discoveredIds = dataStore.Buildings
            .Where(b => b.ImageIds is { Count: > 0 })
            .SelectMany(b => b.ImageIds!)
            .Distinct()
            .ToArray();

        Failures failures = new();
        ConcurrentDictionary<int, byte> survivedIds = new();
        int written = 0;

        await Parallel.ForEachAsync(
            discoveredIds,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency, CancellationToken = cancellationToken },
            async (imageId, ct) =>
            {
                (bool survived, bool writtenNew) = await EnsureImageAsync(imageId, existingIds, failures, ct);
                if (survived)
                {
                    survivedIds[imageId] = 0;
                    if (writtenNew)
                    {
                        Interlocked.Increment(ref written);
                    }
                }
            });

        foreach (BuildingEntity building in dataStore.Buildings)
        {
            if (building.ImageIds is not { Count: > 0 })
            {
                continue;
            }

            // LINQ Where preserves order, so the primary image (index 0) remains primary unless excluded.
            building.ImageIds = building.ImageIds
                .Where(id => survivedIds.ContainsKey(id))
                .ToList();
        }

        int discovered = discoveredIds.Length;
        int survived = survivedIds.Count;
        int existing = survived - written;
        int excluded = discovered - survived;
        bool degraded = discovered > 0 && excluded / (double)discovered >= DegradedExclusionRatio;

        logger.Log(degraded ? LogLevel.Warning : LogLevel.Information,
            "Image sync {Status}. Discovered={Discovered} Existing={Existing} Written={Written} Excluded={Excluded}. Failures: {Failures}",
            degraded ? "degraded" : "complete", discovered, existing, written, excluded, failures.Format());
    }

    private async Task<(bool Survived, bool WrittenNew)> EnsureImageAsync(int imageId, IReadOnlySet<int> existingIds, Failures failures, CancellationToken ct)
    {
        if (existingIds.Contains(imageId))
        {
            await EnsureVariantWarmAsync(imageId, fetchOriginal: ct2 => originalStore.ReadOriginalAsync(imageId, ct2), failures, ct);
            return (Survived: true, WrittenNew: false);
        }

        byte[]? raw = await TryFetchFromPythagorasAsync(imageId, failures, ct);
        if (raw is null)
        {
            return (false, false);
        }

        byte[] normalized;
        try
        {
            normalized = WebPResizer.Resize(raw, MaxOriginalDimension, MaxOriginalDimension, OriginalWebPQuality);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to normalize image {ImageId}.", imageId);
            failures.Record("Normalize", imageId);
            return (false, false);
        }

        try
        {
            await originalStore.WriteOriginalAsync(imageId, normalized, ct);
        }
        catch (ExternalServiceUnavailableException ex)
        {
            logger.LogDebug(ex, "Failed to write original for image {ImageId}.", imageId);
            failures.Record("BlobWrite", imageId);
            return (false, false);
        }

        await EnsureVariantWarmAsync(imageId, fetchOriginal: _ => Task.FromResult(normalized), failures, ct);
        return (Survived: true, WrittenNew: true);
    }

    private async Task EnsureVariantWarmAsync(int imageId, Func<CancellationToken, Task<byte[]>> fetchOriginal, Failures failures, CancellationToken ct)
    {
        try
        {
            await imageService.WarmVariantAsync(
                new ImageVariantRequest($"images:{imageId}", ThumbnailWidth),
                fetchOriginal,
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "Failed to warm 300-wide variant for image {ImageId}.", imageId);
            failures.Record("VariantWarm", imageId);
        }
    }

    private async Task<byte[]?> TryFetchFromPythagorasAsync(int imageId, Failures failures, CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response = await pythagorasClient.GetGalleryImageDataAsync(imageId, GalleryImageVariant.Original, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                failures.Record("PythagorasNotFound", imageId);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogDebug(ex, "Pythagoras returned 404 for image {ImageId}.", imageId);
            failures.Record("PythagorasNotFound", imageId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "Failed to fetch image {ImageId} from Pythagoras.", imageId);
            failures.Record("PythagorasFetch", imageId);
            return null;
        }
    }

    private sealed class Failures
    {
        private readonly ConcurrentDictionary<string, Bucket> _buckets = new();

        public void Record(string category, int imageId)
        {
            Bucket bucket = _buckets.GetOrAdd(category, _ => new Bucket());
            lock (bucket)
            {
                bucket.Count++;
                if (bucket.Samples.Count < SampleIdLimit)
                {
                    bucket.Samples.Add(imageId);
                }
            }
        }

        // Safe to read without locking: only invoked after the parallel loop joins.
        public string Format() => _buckets.IsEmpty
            ? "none"
            : string.Join("; ", _buckets
                .OrderByDescending(kv => kv.Value.Count)
                .Select(kv => $"{kv.Key}={kv.Value.Count} (sample: {string.Join(",", kv.Value.Samples)})"));

        private sealed class Bucket
        {
            public int Count;
            public List<int> Samples { get; } = new(SampleIdLimit);
        }
    }
}
