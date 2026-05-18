namespace Umea.se.Toolkit.Images;

/// <summary>
/// Cached image variant retrieval and warming. Resized variants are encoded using
/// the requested output options and served from FusionCache (L1+L2). Reading the
/// un-resized original is the caller's concern.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Returns the encoded bytes for the variant fitted within the supplied bounds.
    /// <paramref name="loadOriginal"/> is invoked only on cache miss.
    /// </summary>
    Task<ImageVariantBytes> GetVariantAsync(
        ImageVariantRequest request,
        Func<CancellationToken, Task<byte[]>> loadOriginal,
        CancellationToken ct = default);

    /// <summary>
    /// Ensures the variant fitted within the supplied bounds is present in cache.
    /// <paramref name="loadOriginal"/> is invoked only on miss.
    /// </summary>
    Task WarmVariantAsync(
        ImageVariantRequest request,
        Func<CancellationToken, Task<byte[]>> loadOriginal,
        CancellationToken ct = default);
}
