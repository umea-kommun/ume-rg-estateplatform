using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.ServiceAccess.Images;

/// <summary>
/// Azure Blob Storage backed original-image store. Writes normalized WebP bytes
/// under <c>originals/{cacheKeyPrefix}/images/{imageId}/original.webp</c>, mirroring
/// FusionCache's L2 layout (<c>cache/{cacheKeyPrefix}/images/...</c>) so both
/// stores can share the same container with non-overlapping prefixes.
/// </summary>
public sealed class BlobOriginalImageStore(
    BlobContainerClient container,
    ImageServiceOptions imageOptions,
    ILogger<BlobOriginalImageStore> logger) : IOriginalImageStore
{
    private const string ContentType = "image/webp";

    private readonly string _cacheKeyPrefix = imageOptions.CacheKeyPrefix;

    public async Task<byte[]> ReadOriginalAsync(int imageId, CancellationToken cancellationToken = default)
    {
        BlobClient blob = container.GetBlobClient(BuildPath(imageId));
        try
        {
            Response<BlobDownloadResult> response = await blob.DownloadContentAsync(cancellationToken);
            return response.Value.Content.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new ImageNotFoundException($"Image {imageId} not found in original-image store.");
        }
        catch (RequestFailedException ex)
        {
            logger.LogWarning(ex, "Original-image store read failed for image {ImageId}", imageId);
            throw new ExternalServiceUnavailableException(
                $"Original-image store returned {ex.Status} for image {imageId}.", ex);
        }
    }

    public async Task WriteOriginalAsync(int imageId, byte[] webpBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webpBytes);
        BlobClient blob = container.GetBlobClient(BuildPath(imageId));
        try
        {
            using MemoryStream stream = new(webpBytes);
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = ContentType }
            }, cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            logger.LogWarning(ex, "Original-image store write failed for image {ImageId}", imageId);
            throw new ExternalServiceUnavailableException(
                $"Original-image store write returned {ex.Status} for image {imageId}.", ex);
        }
    }

    public async Task<IReadOnlySet<int>> ListExistingImageIdsAsync(CancellationToken cancellationToken = default)
    {
        string prefix = $"originals/{_cacheKeyPrefix}/images/";
        HashSet<int> ids = [];

        try
        {
            await foreach (BlobItem blob in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken))
            {
                ReadOnlySpan<char> remainder = blob.Name.AsSpan(prefix.Length);
                int separator = remainder.IndexOf('/');
                if (separator <= 0)
                {
                    continue;
                }

                if (int.TryParse(remainder[..separator], out int imageId))
                {
                    ids.Add(imageId);
                }
            }
        }
        catch (RequestFailedException ex)
        {
            logger.LogWarning(ex, "Original-image store list failed under prefix {Prefix}", prefix);
            throw new ExternalServiceUnavailableException(
                $"Original-image store returned {ex.Status} when listing originals.", ex);
        }

        return ids;
    }

    private string BuildPath(int imageId) => $"originals/{_cacheKeyPrefix}/images/{imageId}/original.webp";
}
