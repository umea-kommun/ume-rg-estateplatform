using Umea.se.EstateService.Logic.Sync;

namespace Umea.se.EstateService.Test.TestHelpers;

internal sealed class NoOpBuildingImageSyncHandler : IBuildingImageSyncHandler
{
    public Task SyncAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
