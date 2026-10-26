using Microsoft.Extensions.Configuration;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Infrastructure;

public class WorkOrderConfigurationTests
{
    [Fact]
    public void RequiredGroupByType_BindsEnumKeyedDictionary()
    {
        // Pins the config shape: the dictionary is keyed by the WorkOrderType enum *name*
        // ("SpaceRequirement"), matching appsettings.json. If enum-key binding ever breaks the
        // dictionary comes back empty and the gate silently opens, so guard it here. The
        // colon-delimited key is exactly what appsettings.json flattens to.
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Test",
                ["WorkOrder:RequiredGroupByType:SpaceRequirement"] = "9fd6b87f-c7d9-4b5d-8bde-1ee9db7b2c8a",
            })
            .Build();

        ApplicationConfig config = new(configuration);

        config.WorkOrderProcessing.RequiredGroupByType
            .ShouldContainKeyAndValue(WorkOrderType.SpaceRequirement, "9fd6b87f-c7d9-4b5d-8bde-1ee9db7b2c8a");
    }
}
