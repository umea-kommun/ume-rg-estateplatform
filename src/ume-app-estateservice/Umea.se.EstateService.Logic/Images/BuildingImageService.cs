using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Images;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Logic.Images;

public sealed class BuildingImageService(IOriginalImageStore originalStore, IDataStore dataStore, IImageService imageService) : IBuildingImageService
{
    public async Task<ImageVariantBytes?> GetImageAsync(
        int buildingId,
        int? imageId,
        int? maxWidth,
        int? maxHeight,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buildingId);
        if (imageId.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(imageId.Value);
        }

        IReadOnlyList<int>? imageIds = GetImageIds(dataStore, buildingId);

        if (imageIds is null or { Count: 0 })
        {
            return null;
        }

        int resolvedImageId = imageId ?? imageIds[0];

        if (imageId.HasValue && !imageIds.Contains(imageId.Value))
        {
            return null;
        }

        try
        {
            return await imageService.GetVariantAsync(
                new ImageVariantRequest($"images:{resolvedImageId}", maxWidth, maxHeight),
                ct => originalStore.ReadOriginalAsync(resolvedImageId, ct),
                cancellationToken);
        }
        catch (ImageNotFoundException)
        {
            return null;
        }
    }

    public Task<BuildingImageMetadata?> GetImageMetadataAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buildingId);

        IReadOnlyList<int>? imageIds = GetImageIds(dataStore, buildingId);

        if (imageIds is null or { Count: 0 })
        {
            return Task.FromResult<BuildingImageMetadata?>(null);
        }

        return Task.FromResult<BuildingImageMetadata?>(new BuildingImageMetadata(buildingId, imageIds[0], imageIds));
    }

    private static IReadOnlyList<int>? GetImageIds(IDataStore dataStore, int buildingId)
    {
        return dataStore.BuildingsById.TryGetValue(buildingId, out BuildingEntity? building)
            ? building.ImageIds
            : null;
    }
}
