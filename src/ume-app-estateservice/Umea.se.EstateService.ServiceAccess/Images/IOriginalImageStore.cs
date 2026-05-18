using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.ServiceAccess.Images;

/// <summary>
/// Durable storage for normalized building original-image bytes. Source-of-truth
/// for the WebP that variants are resized from. Distinct from FusionCache: never
/// evicts, distinguishes "not found" from "outage".
/// </summary>
public interface IOriginalImageStore
{
    /// <summary>
    /// Read the normalized original WebP bytes for an image.
    /// </summary>
    /// <exception cref="ImageNotFoundException">When the blob does not exist.</exception>
    /// <exception cref="ExternalServiceUnavailableException">
    /// When the underlying blob storage is unavailable (auth, network, throttle).
    /// </exception>
    Task<byte[]> ReadOriginalAsync(int imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write the normalized original WebP bytes for an image. Overwrites any existing blob.
    /// </summary>
    /// <exception cref="ExternalServiceUnavailableException">
    /// When the underlying blob storage is unavailable.
    /// </exception>
    Task WriteOriginalAsync(int imageId, byte[] webpBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// List the imageIds that currently have a normalized original blob. Returned
    /// as a single set so callers can decide existence in O(1) without a per-image
    /// round-trip.
    /// </summary>
    /// <exception cref="ExternalServiceUnavailableException">
    /// When the underlying blob storage is unavailable.
    /// </exception>
    Task<IReadOnlySet<int>> ListExistingImageIdsAsync(CancellationToken cancellationToken = default);
}
