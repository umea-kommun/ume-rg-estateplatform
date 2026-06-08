using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Sync.Pythagoras;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Handlers.Blueprint;
using Umea.se.EstateService.Logic.Handlers.Favorite;
using Umea.se.EstateService.Logic.Images;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Logic.Sync;
using Umea.se.EstateService.Logic.Search.Providers;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Infrastructure;

namespace Umea.se.EstateService.Logic;

public static class DependencyInjectionLogic
{
    public static IServiceCollection AddLogicDependencies(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryDataStore>();
        services.AddSingleton<IDataStore>(sp => sp.GetRequiredService<InMemoryDataStore>());
        services.AddSingleton<IDataRefreshService, PythagorasDataRefreshService>();

        services.AddSingleton<IEstateDataQueryHandler, EstateDataQueryHandler>();
        services.AddSingleton<SearchHandler>();
        services.AddSingleton<IIndexedPythagorasDocumentReader>(sp => sp.GetRequiredService<SearchHandler>());
        services.AddSingleton<IPythagorasDocumentProvider, DataStoreDocumentProvider>();

        services.AddSingleton<IFloorBlueprintService, FloorBlueprintHandler>();
        services.AddScoped<IBuildingImageService, BuildingImageService>();
        services.AddSingleton<DocumentSyncHandler>();
        services.AddSingleton<IBuildingImageSyncHandler, BuildingImageSyncHandler>();

        services.AddTransient<IFileDocumentHandler, FileDocumentHandler>();

        services.AddSingleton<WorkOrderChannel>();
        services.AddScoped<WorkOrderStatusSyncService>();
        services.AddScoped<WorkOrderProcessor>();
        services.AddSingleton<WorkOrderFileValidator>();
        services.AddSingleton<WorkOrderCategoryProvider>();
        services.AddSingleton<WorkOrderAccessPolicy>(sp =>
            new WorkOrderAccessPolicy(sp.GetRequiredService<ApplicationConfig>().WorkOrderProcessing));
        services.AddScoped<IWorkOrderHandler, WorkOrderHandler>();
        services.AddScoped<IFavoriteHandler, FavoriteHandler>();

        services.AddSingleton<RefreshPipelineRunner>();
        services.AddSingleton<DataSyncService>();
        services.AddHostedService(sp => sp.GetRequiredService<DataSyncService>());
        services.AddHostedService<WorkOrderProcessingService>();

        return services;
    }
}
