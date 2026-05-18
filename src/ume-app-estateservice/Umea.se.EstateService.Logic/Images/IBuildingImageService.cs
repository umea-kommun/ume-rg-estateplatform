using Umea.se.EstateService.Logic.Models;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Logic.Images;

/// <summary>
/// Service for building image operations.
/// </summary>
public interface IBuildingImageService
{
    /// <summary>
    /// Gets encoded bytes for a building image with optional resizing. Reads from the
    /// original-image store; returns null if the building has no images, the
    /// requested imageId does not belong to the building, or the original blob
    /// is missing.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="imageId">Optional image ID. If null, returns the primary (most recently updated) image.</param>
    /// <param name="maxWidth">Optional maximum width in pixels</param>
    /// <param name="maxHeight">Optional maximum height in pixels</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<ImageVariantBytes?> GetImageAsync(
        int buildingId,
        int? imageId,
        int? maxWidth,
        int? maxHeight,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about all images for a building.
    /// </summary>
    Task<BuildingImageMetadata?> GetImageMetadataAsync(int buildingId, CancellationToken cancellationToken = default);
}
