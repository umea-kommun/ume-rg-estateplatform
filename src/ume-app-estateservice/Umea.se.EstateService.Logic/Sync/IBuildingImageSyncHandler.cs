namespace Umea.se.EstateService.Logic.Sync;

/// <summary>
/// Sync step that ensures every advertised building image has a normalized origin
/// blob and a warm 300-wide variant in FusionCache.
/// </summary>
public interface IBuildingImageSyncHandler
{
    Task SyncAsync(CancellationToken cancellationToken);
}
