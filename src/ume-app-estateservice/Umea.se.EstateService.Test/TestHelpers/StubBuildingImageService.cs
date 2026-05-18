using Umea.se.EstateService.Logic.Images;
using Umea.se.EstateService.Logic.Models;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Simple stub for <see cref="IBuildingImageService"/> that returns configured results.
/// Use this for controller tests where you just need to verify the response handling.
/// </summary>
public sealed class StubBuildingImageService : IBuildingImageService
{
    /// <summary>
    /// The bytes to return from <see cref="GetImageAsync"/>.
    /// Set to null to simulate no image found.
    /// </summary>
    public byte[]? ImageBytes { get; set; }

    /// <summary>
    /// The result to return from <see cref="GetImageMetadataAsync"/>.
    /// Set to null to simulate no images found.
    /// </summary>
    public BuildingImageMetadata? MetadataResult { get; set; }

    /// <summary>
    /// Captured building IDs from <see cref="GetImageAsync"/> calls.
    /// </summary>
    public List<int> ImageRequestedForBuildingIds { get; } = [];

    /// <summary>
    /// Captured building IDs from <see cref="GetImageMetadataAsync"/> calls.
    /// </summary>
    public List<int> MetadataRequestedForBuildingIds { get; } = [];

    public Task<ImageVariantBytes?> GetImageAsync(
        int buildingId,
        int? imageId,
        int? maxWidth,
        int? maxHeight,
        CancellationToken cancellationToken = default)
    {
        ImageRequestedForBuildingIds.Add(buildingId);
        ImageVariantBytes? result = ImageBytes is null
            ? null
            : new ImageVariantBytes(ImageBytes, "image/webp");
        return Task.FromResult(result);
    }

    public Task<BuildingImageMetadata?> GetImageMetadataAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        MetadataRequestedForBuildingIds.Add(buildingId);
        return Task.FromResult(MetadataResult);
    }

    /// <summary>
    /// Resets all configured results and captured requests.
    /// </summary>
    public void Reset()
    {
        ImageBytes = null;
        MetadataResult = null;
        ImageRequestedForBuildingIds.Clear();
        MetadataRequestedForBuildingIds.Clear();
    }
}
