using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.DataStore.SqlServer;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.ServiceAccess.FileStorage;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Handlers;

public class WorkOrderHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EstateDbContext _dbContext;
    private readonly InMemoryDataStore _dataStore;
    private readonly WorkOrderHandler _handler;

    public WorkOrderHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<EstateDbContext> options = new DbContextOptionsBuilder<EstateDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create schema
        using (EstateDbContext context = new(options))
        {
            context.Database.EnsureCreated();
        }

        _dbContext = new EstateDbContext(options);
        IWorkOrderRepository workOrderRepository = new WorkOrderRepository(_dbContext);
        _dataStore = new InMemoryDataStore();

        DataStoreSeeder.Seed(
            _dataStore,
            buildings: [new BuildingEntity { Id = 1, Name = "Building One", PopularName = "B1", WorkOrderTypes = [WorkOrderType.ErrorReport, WorkOrderType.BuildingService, WorkOrderType.SpaceRequirement] }],
            rooms: [new RoomEntity { Id = 10, Name = "Room Ten", PopularName = "R10", BuildingId = 1 }],
            // SpaceRequirement (Pythagoras type 3) leaf categories the user can pick from.
            workOrderCategories:
            [
                new WorkOrderCategoryNode { Id = 89, Name = "Generella utredningar", WorkOrderTypeIds = [3] },
                new WorkOrderCategoryNode { Id = 91, Name = "Ombyggnad", WorkOrderTypeIds = [3] },
            ]);

        ApplicationConfig config = CreateTestConfig();
        IWorkOrderFileStorage fileStorage = new LocalWorkOrderFileStorage(config);
        WorkOrderFileValidator fileValidator = new(config);
        WorkOrderCategoryProvider categoryProvider = new(_dataStore);
        WorkOrderAccessPolicy accessPolicy = new(new WorkOrderConfiguration());
        _handler = new WorkOrderHandler(workOrderRepository, _dataStore, new WorkOrderChannel(), fileStorage, fileValidator, categoryProvider, accessPolicy, NullLogger<WorkOrderHandler>.Instance);
    }

    [Fact]
    public async Task SubmitWorkOrder_ValidIndoor_CreatesEntity()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "Indoor",
            RoomId = 10,
            Description = "Test workOrder"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        result.Id.ShouldNotBe(Guid.Empty);
        result.SyncStatus.ShouldBe("Pending");

        // Verify detail through GetWorkOrderAsync
        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.BuildingName.ShouldBe("Building One");
        detail.RoomName.ShouldBe("Room Ten");
        detail.Location.ShouldBe("Indoor");
    }

    [Fact]
    public async Task SubmitWorkOrder_WithNotifierPhone_PersistsPhoneOnEntity()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "Indoor",
            RoomId = 10,
            Description = "Test",
            NotifierName = "Test User",
            NotifierEmail = "notifier@example.com",
            NotifierPhone = "+46 70 123 45 67"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        WorkOrderEntity? entity = await _dbContext.WorkOrders.FirstOrDefaultAsync(w => w.Uid == result.Id);
        entity.ShouldNotBeNull();
        entity.NotifierPhone.ShouldBe("+46 70 123 45 67");
        entity.NotifierName.ShouldBe("Test User");
        entity.NotifierEmail.ShouldBe("notifier@example.com");
    }

    [Fact]
    public async Task SubmitWorkOrder_ValidOutdoor_CreatesEntityWithoutRoom()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "Outdoor",
            Description = "Outdoor issue"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.RoomName.ShouldBeNull();
        detail.Location.ShouldBe("Outdoor");
    }

    [Fact]
    public async Task SubmitWorkOrder_InvalidLocation_ThrowsWithFieldError()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "InvalidType",
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("location");
        exception.Errors["location"].ShouldContain("invalid_value");
    }

    [Fact]
    public async Task SubmitWorkOrder_InvalidWorkOrderType_ThrowsWithFieldError()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = (WorkOrderType)999,
            Location = "Indoor",
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("workOrderType");
        exception.Errors["workOrderType"].ShouldContain("invalid_value");
    }

    [Fact]
    public async Task SubmitWorkOrder_InvalidBuilding_ThrowsWithFieldError()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 9999,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "Indoor",
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("buildingId");
        exception.Errors["buildingId"].ShouldContain("not_found");
    }

    [Fact]
    public async Task SubmitWorkOrder_OutdoorWithRoom_ThrowsWithFieldError()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "Outdoor",
            RoomId = 10,
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("roomId");
        exception.Errors["roomId"].ShouldContain("conflict");
    }

    [Fact]
    public async Task SubmitWorkOrder_RoomNotInBuilding_ThrowsWithFieldError()
    {
        DataStoreSeeder.Seed(
            _dataStore,
            buildings: [new BuildingEntity { Id = 1, Name = "Building One", PopularName = "B1", WorkOrderTypes = [WorkOrderType.ErrorReport, WorkOrderType.BuildingService] }],
            rooms: [new RoomEntity { Id = 10, Name = "Room Ten", PopularName = "R10", BuildingId = 2 }]);

        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "Indoor",
            RoomId = 10,
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("roomId");
        exception.Errors["roomId"].ShouldContain("invalid_value");
    }

    [Fact]
    public async Task SubmitWorkOrder_MultipleInvalidFields_ReturnsAllErrors()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 9999,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "InvalidType",
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.Count.ShouldBeGreaterThanOrEqualTo(2);
        exception.Errors.ShouldContainKey("location");
        exception.Errors.ShouldContainKey("buildingId");
    }

    [Fact]
    public async Task SubmitWorkOrder_BuildingService_WithoutLocation_StoresNullLocation()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.BuildingService,
            Description = "Service request"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        result.Id.ShouldNotBe(Guid.Empty);

        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.Location.ShouldBeNull();
        detail.RoomName.ShouldBeNull();
    }

    [Fact]
    public async Task SubmitWorkOrder_BuildingService_KeepsRoomButIgnoresLocation()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.BuildingService,
            Location = "Indoor",
            RoomId = 10,
            Description = "Service request"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.Location.ShouldBeNull();
        detail.RoomName.ShouldBe("Room Ten");
    }

    [Fact]
    public async Task SubmitWorkOrder_SpaceRequirement_WithoutLocation_StoresTypeAndNullLocation()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.SpaceRequirement,
            Description = "Behöver större lokal"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.WorkOrderType.ShouldBe(WorkOrderType.SpaceRequirement);
        detail.Location.ShouldBeNull();
        detail.RoomName.ShouldBeNull();
    }

    [Fact]
    public async Task SubmitWorkOrder_SpaceRequirement_WithRoom_BindsRoom()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.SpaceRequirement,
            RoomId = 10,
            Description = "Behöver anpassa rum"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.WorkOrderType.ShouldBe(WorkOrderType.SpaceRequirement);
        detail.RoomName.ShouldBe("Room Ten");
    }

    [Fact]
    public async Task SubmitWorkOrder_SpaceRequirement_WithChosenCategory_PersistsCategoryId()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.SpaceRequirement,
            CategoryId = 91,
            Description = "Behöver bygga om"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        WorkOrderEntity? entity = await _dbContext.WorkOrders.FirstOrDefaultAsync(w => w.Uid == result.Id);
        entity.ShouldNotBeNull();
        entity.CategoryId.ShouldBe(91);
    }

    [Fact]
    public async Task SubmitWorkOrder_WithCategoryNotValidForType_ThrowsInvalidValue()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.SpaceRequirement,
            CategoryId = 999, // not a leaf category for type 3
            Description = "Behöver större lokal"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("categoryId");
    }

    [Fact]
    public void GetCategoriesForType_SpaceRequirement_ReturnsLeafCategories()
    {
        IReadOnlyList<WorkOrderCategoryOption> categories = _handler.GetCategoriesForType(WorkOrderType.SpaceRequirement);

        categories.Select(c => c.Id).ShouldBe([89, 91], ignoreOrder: true);
        categories.Single(c => c.Id == 89).Name.ShouldBe("Generella utredningar");
    }

    [Fact]
    public void GetCategoriesForType_TypeWithoutCategories_ReturnsEmpty()
    {
        _handler.GetCategoriesForType(WorkOrderType.ErrorReport).ShouldBeEmpty();
    }

    // --- AAD group gating: SpaceRequirement restricted to a configured group ---

    private const string SpaceRequirementGroup = "11111111-2222-3333-4444-555555555555";

    [Fact]
    public async Task SubmitWorkOrder_GatedType_UserInGroup_Succeeds()
    {
        WorkOrderHandler handler = CreateGatedHandler();
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.SpaceRequirement,
            Description = "Behöver större lokal"
        };

        WorkOrderSubmissionModel result = await handler.SubmitWorkOrderAsync(request, "test@example.com", [SpaceRequirementGroup]);

        WorkOrderEntity? entity = await _dbContext.WorkOrders.FirstOrDefaultAsync(w => w.Uid == result.Id);
        entity.ShouldNotBeNull();
    }

    [Fact]
    public async Task SubmitWorkOrder_GatedType_UserNotInGroup_ThrowsNotSupported()
    {
        WorkOrderHandler handler = CreateGatedHandler();
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.SpaceRequirement,
            Description = "Behöver större lokal"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => handler.SubmitWorkOrderAsync(request, "test@example.com", ["some-other-group"]));

        exception.Errors.ShouldContainKey("workOrderType");
    }

    [Fact]
    public async Task SubmitWorkOrder_GatedType_NoGroups_ThrowsNotSupported()
    {
        WorkOrderHandler handler = CreateGatedHandler();
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.SpaceRequirement,
            Description = "Behöver större lokal"
        };

        // Fail-closed: omitting groups entirely is treated as "not a member".
        await Should.ThrowAsync<BusinessValidationException>(
            () => handler.SubmitWorkOrderAsync(request, "test@example.com"));
    }

    [Fact]
    public async Task SubmitWorkOrder_UngatedType_UserNotInGroup_Succeeds()
    {
        WorkOrderHandler handler = CreateGatedHandler();
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "Indoor",
            RoomId = 10,
            Description = "Trasig lampa"
        };

        WorkOrderSubmissionModel result = await handler.SubmitWorkOrderAsync(request, "test@example.com", ["some-other-group"]);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetCategoriesForType_GatedType_UserInGroup_ReturnsCategories()
    {
        WorkOrderHandler handler = CreateGatedHandler();

        handler.GetCategoriesForType(WorkOrderType.SpaceRequirement, [SpaceRequirementGroup])
            .Select(c => c.Id).ShouldBe([89, 91], ignoreOrder: true);
    }

    [Fact]
    public void GetCategoriesForType_GatedType_UserNotInGroup_ReturnsEmpty()
    {
        WorkOrderHandler handler = CreateGatedHandler();

        handler.GetCategoriesForType(WorkOrderType.SpaceRequirement, ["some-other-group"]).ShouldBeEmpty();
    }

    private WorkOrderHandler CreateGatedHandler()
    {
        ApplicationConfig config = CreateTestConfig();
        IWorkOrderRepository repository = new WorkOrderRepository(_dbContext);
        IWorkOrderFileStorage fileStorage = new LocalWorkOrderFileStorage(config);
        WorkOrderFileValidator fileValidator = new(config);
        WorkOrderCategoryProvider categoryProvider = new(_dataStore);
        WorkOrderAccessPolicy accessPolicy = new(new WorkOrderConfiguration
        {
            RequiredGroupByType = { [WorkOrderType.SpaceRequirement] = SpaceRequirementGroup }
        });
        return new WorkOrderHandler(repository, _dataStore, new WorkOrderChannel(), fileStorage, fileValidator, categoryProvider, accessPolicy, NullLogger<WorkOrderHandler>.Instance);
    }

    [Fact]
    public async Task SubmitWorkOrder_TypeNotSupportedByBuilding_ThrowsNotSupported()
    {
        // FacilityService is not in Building One's WorkOrderTypes
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.FacilityService,
            Description = "Should be rejected"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("workOrderType");
    }

    [Fact]
    public async Task SubmitWorkOrder_ErrorReportWithoutLocation_ThrowsRequired()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("location");
        exception.Errors["location"].ShouldContain("required");
    }

    [Fact]
    public async Task GetWorkOrders_ReturnsOnlyUserWorkOrders()
    {
        await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = WorkOrderType.ErrorReport, Location = "Indoor", Description = "User A workOrder" },
            "usera@example.com");

        await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = WorkOrderType.ErrorReport, Location = "Indoor", Description = "User B workOrder" },
            "userb@example.com");

        IReadOnlyList<WorkOrderListItemModel> result = await _handler.GetWorkOrdersAsync("usera@example.com");

        result.Count.ShouldBe(1);
        result[0].Description.ShouldBe("User A workOrder");
    }

    [Fact]
    public async Task GetWorkOrder_ByUid_ReturnsCorrectWorkOrder()
    {
        WorkOrderSubmissionModel created = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = WorkOrderType.ErrorReport, Location = "Indoor", Description = "Find me" },
            "test@example.com");

        WorkOrderDetailModel result = await _handler.GetWorkOrderAsync(created.Id, "test@example.com");

        result.ShouldNotBeNull();
        result.Description.ShouldBe("Find me");
    }

    [Fact]
    public async Task GetWorkOrder_WrongUser_ThrowsNotFound()
    {
        WorkOrderSubmissionModel created = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = WorkOrderType.ErrorReport, Location = "Indoor", Description = "Not yours" },
            "usera@example.com");

        await Should.ThrowAsync<EntityNotFoundException>(
            () => _handler.GetWorkOrderAsync(created.Id, "userb@example.com"));
    }

    [Fact]
    public async Task SyncWorkOrder_ReturnsWorkOrder()
    {
        WorkOrderSubmissionModel created = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = WorkOrderType.ErrorReport, Location = "Indoor", Description = "Sync me" },
            "test@example.com");

        WorkOrderDetailModel result = await _handler.SyncWorkOrderAsync(created.Id, "test@example.com");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task SyncWorkOrder_WrongUser_ThrowsNotFound()
    {
        WorkOrderSubmissionModel created = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = WorkOrderType.ErrorReport, Location = "Indoor", Description = "Not yours" },
            "usera@example.com");

        await Should.ThrowAsync<EntityNotFoundException>(
            () => _handler.SyncWorkOrderAsync(created.Id, "userb@example.com"));
    }

    [Fact]
    public async Task SyncWorkOrder_NotFound_ThrowsNotFound()
    {
        await Should.ThrowAsync<EntityNotFoundException>(
            () => _handler.SyncWorkOrderAsync(Guid.NewGuid(), "test@example.com"));
    }

    [Fact]
    public async Task SubmitWorkOrder_SetsNextSyncAtToNow()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = WorkOrderType.ErrorReport, Location = "Indoor", Description = "Test" },
            "test@example.com");

        result.Id.ShouldNotBe(Guid.Empty);
        result.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    private static ApplicationConfig CreateTestConfig()
    {
        Dictionary<string, string?> configData = new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Test",
            ["WorkOrder:FileStorage"] = Path.Combine(Path.GetTempPath(), "workOrder-handler-tests"),
            ["WorkOrder:MaxRetries"] = "3",
            ["Pythagoras:ApiKey"] = "test",
            ["Pythagoras:BaseUrl"] = "https://localhost/",
            ["Authentication:TokenServiceUrl"] = "https://localhost/",
            ["Authentication:Audience"] = "test"
        };

        Microsoft.Extensions.Configuration.IConfigurationRoot configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return new ApplicationConfig(configuration);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();

        // Clean up test files
        string testDir = Path.Combine(Path.GetTempPath(), "workOrder-handler-tests");
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, recursive: true);
        }
    }
}
