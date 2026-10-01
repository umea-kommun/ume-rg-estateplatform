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
using Umea.se.EstateService.Shared.Data.Enums;
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
    private readonly FakePythagorasClient _statusClient = new();

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
        _dataStore = new InMemoryDataStore();

        DataStoreSeeder.Seed(
            _dataStore,
            buildings:
            [
                new BuildingEntity { Id = 1, Name = "Building One", PopularName = "B1", ImageIds = [7], WorkOrderTypes = [WorkOrderType.ErrorReport, WorkOrderType.BuildingService, WorkOrderType.SpaceRequirement] },
                new BuildingEntity { Id = 2, Name = "Building Two", PopularName = "B2", ImageIds = [] },
            ],
            rooms: [new RoomEntity { Id = 10, Name = "Room Ten", PopularName = "R10", BuildingId = 1 }],
            // SpaceRequirement (Pythagoras type 3) leaf categories the user can pick from.
            workOrderCategories:
            [
                new WorkOrderCategoryNode { Id = 89, Name = "Generella utredningar", WorkOrderTypeIds = [3] },
                new WorkOrderCategoryNode { Id = 91, Name = "Ombyggnad", WorkOrderTypeIds = [3] },
            ]);

        _handler = CreateHandler();
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
        detail.BuildingName.ShouldBe("B1");
        detail.RoomName.ShouldBe("Room Ten");
        detail.Location.ShouldBe("Indoor");
    }

    [Theory]
    [InlineData("", "Building One")]
    [InlineData("   ", "Building One")]
    [InlineData("B1", "B1")]
    public async Task SubmitWorkOrder_SavesPopularNameAsBuildingNameAndFallsBackToName(string popularName, string expected)
    {
        DataStoreSeeder.Seed(
            _dataStore,
            buildings: [new BuildingEntity { Id = 1, Name = "Building One", PopularName = popularName, WorkOrderTypes = [WorkOrderType.ErrorReport] }]);

        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = WorkOrderType.ErrorReport,
            Location = "Indoor",
            Description = "Test workOrder"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        (await ReloadAsync(result.Id)).BuildingName.ShouldBe(expected);
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
    public async Task SubmitWorkOrder_SpaceRequirement_WithoutBuilding_Succeeds()
    {
        CreateWorkOrderRequest request = new()
        {
            // No BuildingId: SpaceRequirement may be submitted without a building.
            WorkOrderType = WorkOrderType.SpaceRequirement,
            CategoryId = 89,
            Description = "Behöver en helt ny lokal"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.WorkOrderType.ShouldBe(WorkOrderType.SpaceRequirement);
        detail.BuildingName.ShouldBeNull();
        detail.RoomName.ShouldBeNull();
    }

    [Fact]
    public async Task SubmitWorkOrder_NonSpaceRequirement_WithoutBuilding_ThrowsRequired()
    {
        CreateWorkOrderRequest request = new()
        {
            // No BuildingId: building stays mandatory for every type except SpaceRequirement.
            WorkOrderType = WorkOrderType.BuildingService,
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("buildingId");
        exception.Errors["buildingId"].ShouldContain("required");
    }

    [Fact]
    public async Task SubmitWorkOrder_SpaceRequirement_RoomWithoutBuilding_ThrowsWithFieldError()
    {
        CreateWorkOrderRequest request = new()
        {
            // A room can't be bound without its building.
            WorkOrderType = WorkOrderType.SpaceRequirement,
            RoomId = 10,
            Description = "Test"
        };

        BusinessValidationException exception = await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));

        exception.Errors.ShouldContainKey("roomId");
        exception.Errors["roomId"].ShouldContain("invalid_value");
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

    private WorkOrderHandler CreateGatedHandler() => CreateHandler(accessConfig: new WorkOrderConfiguration
    {
        RequiredGroupByType = { [WorkOrderType.SpaceRequirement] = SpaceRequirementGroup }
    });

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
    public async Task GetWorkOrders_MapsBuildingDetailsAndWorkOrderNumber()
    {
        WorkOrderEntity withImages = await InsertWorkOrderAsync(WorkOrderSyncStatus.Submitted, null, description: new string('a', 300));
        withImages.PythagorasWorkOrderName = "UK-2026-2121";
        WorkOrderEntity withoutImages = await InsertWorkOrderAsync(WorkOrderSyncStatus.Submitted, null);
        withoutImages.BuildingId = 2;
        WorkOrderEntity unknownBuilding = await InsertWorkOrderAsync(WorkOrderSyncStatus.Submitted, null);
        unknownBuilding.BuildingId = 99;
        WorkOrderEntity noBuilding = await InsertWorkOrderAsync(WorkOrderSyncStatus.Submitted, null);
        noBuilding.BuildingId = null;
        noBuilding.BuildingName = null;
        await _dbContext.SaveChangesAsync();

        IReadOnlyList<WorkOrderListItemModel> result = await _handler.GetWorkOrdersAsync("test@example.com");

        WorkOrderListItemModel first = result.Single(e => e.Id == withImages.Uid);
        first.WorkOrderNumber.ShouldBe("UK-2026-2121");
        first.BuildingId.ShouldBe(1);
        first.BuildingPopularName.ShouldBe("B1");
        first.BuildingImageUrl.ShouldBe("/api/buildings/1/image");
        first.Description.Length.ShouldBe(300);

        WorkOrderListItemModel second = result.Single(e => e.Id == withoutImages.Uid);
        second.BuildingPopularName.ShouldBe("B2");
        second.BuildingImageUrl.ShouldBeNull();

        WorkOrderListItemModel unknown = result.Single(e => e.Id == unknownBuilding.Uid);
        unknown.BuildingId.ShouldBe(99);
        unknown.BuildingPopularName.ShouldBeNull();
        unknown.BuildingImageUrl.ShouldBeNull();

        WorkOrderListItemModel none = result.Single(e => e.Id == noBuilding.Uid);
        none.BuildingId.ShouldBeNull();
        none.WorkOrderNumber.ShouldBeNull();
        none.BuildingPopularName.ShouldBeNull();
        none.BuildingImageUrl.ShouldBeNull();
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
    public async Task SyncWorkOrder_StatusSyncEnabled_ReturnsRefreshedStatus()
    {
        WorkOrderEntity submitted = await InsertSubmittedAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);
        _dbContext.ChangeTracker.Clear();

        WorkOrderDetailModel result = await CreateHandler().SyncWorkOrderAsync(submitted.Uid, "test@example.com");

        result.Status.ShouldBe("Pågår");
        result.RefreshOutcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);
        result.StatusCheckedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SyncWorkOrder_StatusSyncDisabled_IsNoOp()
    {
        DateTimeOffset scheduled = DateTimeOffset.UtcNow.AddHours(1);
        WorkOrderEntity submitted = await InsertSubmittedAsync(nextSyncAt: scheduled);

        WorkOrderDetailModel result = await CreateHandler(CreateTestConfig(statusSyncEnabled: false))
            .SyncWorkOrderAsync(submitted.Uid, "test@example.com");

        result.Id.ShouldBe(submitted.Uid);

        WorkOrderEntity reloaded = await ReloadAsync(submitted.Uid);
        reloaded.NextSyncAt!.Value.ShouldBe(scheduled, TimeSpan.FromSeconds(1));
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

    // --- Existing user-facing retry: now keeps ErrorMessage while the retry is in flight ---

    [Fact]
    public async Task RetryWorkOrder_PermanentlyFailed_ResetsButKeepsErrorMessage()
    {
        WorkOrderEntity failed = await InsertWorkOrderAsync(
            WorkOrderSyncStatus.Failed, nextSyncAt: null, errorMessage: "Pythagoras rejected the request", retryCount: 3);

        WorkOrderDetailModel result = await _handler.RetryWorkOrderAsync(failed.Uid, "test@example.com");

        result.SyncStatus.ShouldBe("Pending");
        result.ErrorMessage.ShouldBe("Pythagoras rejected the request");

        WorkOrderEntity reloaded = await ReloadAsync(failed.Uid);
        reloaded.SyncStatus.ShouldBe(WorkOrderSyncStatus.Pending);
        reloaded.RetryCount.ShouldBe(0);
        reloaded.NextSyncAt.ShouldNotBeNull();
        reloaded.ErrorMessage.ShouldBe("Pythagoras rejected the request");
    }

    // --- Admin: failed work order monitoring & remediation ---

    [Fact]
    public async Task GetFailedCount_CountsOnlyPermanentlyFailed()
    {
        await InsertWorkOrderAsync(WorkOrderSyncStatus.Failed, nextSyncAt: null); // permanently failed
        await InsertWorkOrderAsync(WorkOrderSyncStatus.Failed, nextSyncAt: null); // permanently failed
        await InsertWorkOrderAsync(WorkOrderSyncStatus.Failed, nextSyncAt: DateTimeOffset.UtcNow); // retry scheduled
        await InsertWorkOrderAsync(WorkOrderSyncStatus.Pending, nextSyncAt: DateTimeOffset.UtcNow);
        await InsertWorkOrderAsync(WorkOrderSyncStatus.Dismissed, nextSyncAt: null);

        (await _handler.GetFailedCountAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task GetFailedWorkOrders_ReturnsOnlyPermanentlyFailed_OrderedByUpdatedAtDesc()
    {
        WorkOrderEntity older = await InsertWorkOrderAsync(
            WorkOrderSyncStatus.Failed, nextSyncAt: null, description: "older", updatedAt: DateTimeOffset.UtcNow.AddHours(-2));
        WorkOrderEntity newer = await InsertWorkOrderAsync(
            WorkOrderSyncStatus.Failed, nextSyncAt: null, description: "newer", updatedAt: DateTimeOffset.UtcNow.AddHours(-1));
        await InsertWorkOrderAsync(WorkOrderSyncStatus.Failed, nextSyncAt: DateTimeOffset.UtcNow, description: "retry scheduled");
        await InsertWorkOrderAsync(WorkOrderSyncStatus.Dismissed, nextSyncAt: null, description: "dismissed");

        IReadOnlyList<FailedWorkOrderModel> result = await _handler.GetFailedWorkOrdersAsync();

        result.Select(r => r.Id).ShouldBe([newer.Uid, older.Uid]);
    }

    [Fact]
    public async Task GetFailedWorkOrders_MapsDiagnosticFields()
    {
        WorkOrderEntity failed = await InsertWorkOrderAsync(
            WorkOrderSyncStatus.Failed, nextSyncAt: null, errorMessage: "boom", retryCount: 3, description: "broken");

        FailedWorkOrderModel model = (await _handler.GetFailedWorkOrdersAsync()).Single();

        model.Id.ShouldBe(failed.Uid);
        model.BuildingName.ShouldBe("Building One");
        model.Description.ShouldBe("broken");
        model.SyncStatus.ShouldBe("Failed");
        model.ErrorMessage.ShouldBe("boom");
        model.RetryCount.ShouldBe(3);
    }

    [Fact]
    public async Task AdminRetry_PermanentlyFailed_ResetsButKeepsErrorMessage()
    {
        WorkOrderEntity failed = await InsertWorkOrderAsync(
            WorkOrderSyncStatus.Failed, nextSyncAt: null, errorMessage: "boom", retryCount: 3);

        FailedWorkOrderModel result = await _handler.AdminRetryWorkOrderAsync(failed.Uid);

        result.SyncStatus.ShouldBe("Pending");
        result.ErrorMessage.ShouldBe("boom");

        WorkOrderEntity reloaded = await ReloadAsync(failed.Uid);
        reloaded.SyncStatus.ShouldBe(WorkOrderSyncStatus.Pending);
        reloaded.RetryCount.ShouldBe(0);
        reloaded.NextSyncAt.ShouldNotBeNull();
        reloaded.ErrorMessage.ShouldBe("boom");
    }

    [Fact]
    public async Task AdminRetry_NotFailed_ThrowsStateConflict()
    {
        WorkOrderEntity pending = await InsertWorkOrderAsync(WorkOrderSyncStatus.Pending, nextSyncAt: DateTimeOffset.UtcNow);

        await Should.ThrowAsync<StateConflictException>(() => _handler.AdminRetryWorkOrderAsync(pending.Uid));
    }

    [Fact]
    public async Task AdminRetry_RetryScheduled_ThrowsStateConflict()
    {
        // Failed but with a pending retry (NextSyncAt set) is not "permanently" failed.
        WorkOrderEntity transient = await InsertWorkOrderAsync(WorkOrderSyncStatus.Failed, nextSyncAt: DateTimeOffset.UtcNow);

        await Should.ThrowAsync<StateConflictException>(() => _handler.AdminRetryWorkOrderAsync(transient.Uid));
    }

    [Fact]
    public async Task AdminRetry_NotFound_ThrowsNotFound()
    {
        await Should.ThrowAsync<EntityNotFoundException>(() => _handler.AdminRetryWorkOrderAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AdminDismiss_PermanentlyFailed_SetsDismissedAndKeepsErrorMessage()
    {
        WorkOrderEntity failed = await InsertWorkOrderAsync(
            WorkOrderSyncStatus.Failed, nextSyncAt: null, errorMessage: "boom", retryCount: 3);

        FailedWorkOrderModel result = await _handler.AdminDismissWorkOrderAsync(failed.Uid);

        result.SyncStatus.ShouldBe("Dismissed");
        result.ErrorMessage.ShouldBe("boom");

        WorkOrderEntity reloaded = await ReloadAsync(failed.Uid);
        reloaded.SyncStatus.ShouldBe(WorkOrderSyncStatus.Dismissed);
        reloaded.NextSyncAt.ShouldBeNull();
        reloaded.ErrorMessage.ShouldBe("boom");
    }

    [Fact]
    public async Task AdminDismiss_NotFailed_ThrowsStateConflict()
    {
        WorkOrderEntity submitted = await InsertWorkOrderAsync(WorkOrderSyncStatus.Submitted, nextSyncAt: null);

        await Should.ThrowAsync<StateConflictException>(() => _handler.AdminDismissWorkOrderAsync(submitted.Uid));
    }

    [Fact]
    public async Task AdminDismiss_NotFound_ThrowsNotFound()
    {
        await Should.ThrowAsync<EntityNotFoundException>(() => _handler.AdminDismissWorkOrderAsync(Guid.NewGuid()));
    }

    private async Task<WorkOrderEntity> InsertWorkOrderAsync(
        WorkOrderSyncStatus syncStatus,
        DateTimeOffset? nextSyncAt,
        string? errorMessage = null,
        int retryCount = 0,
        DateTimeOffset? updatedAt = null,
        string description = "Failed order")
    {
        WorkOrderEntity entity = new()
        {
            Uid = Guid.NewGuid(),
            BuildingId = 1,
            BuildingName = "Building One",
            Description = description,
            WorkOrderTypeId = 1,
            SyncStatus = syncStatus,
            NextSyncAt = nextSyncAt,
            ErrorMessage = errorMessage,
            RetryCount = retryCount,
            CreatedByEmail = "test@example.com",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow
        };
        _dbContext.WorkOrders.Add(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    [Fact]
    public async Task SyncWorkOrders_RefreshesOnlyOwnersDueOrders_AndUsesInfoBatch()
    {
        WorkOrderEntity due = await InsertSubmittedAsync();
        WorkOrderEntity recent = await InsertSubmittedAsync();
        recent.PythagorasWorkOrderId = 556;
        recent.StatusCheckedAt = DateTimeOffset.UtcNow;
        WorkOrderEntity other = await InsertSubmittedAsync();
        other.CreatedByEmail = "other@example.com";
        other.PythagorasWorkOrderId = 557;
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår", StatusCategory = "ONGOING" }]);

        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);
        result.WorkOrders.Count.ShouldBe(2);
        _statusClient.WorkOrderRequests.Count.ShouldBe(1);
        _statusClient.WorkOrderRequests[0].Method.ShouldBe("GetWorkOrderInfosByIds");
        _statusClient.WorkOrderRequests[0].Parameters.ShouldBe("ids=555");
        WorkOrderEntity reloaded = await ReloadAsync(due.Uid);
        reloaded.PythagorasStatusCategory.ShouldBe("ONGOING");
        result.WorkOrders.Single(e => e.Id == due.Uid).DisplayStatus.ShouldBe(WorkOrderDisplayStatus.InProgress);
        reloaded.StatusCheckedAt.ShouldNotBeNull();
        reloaded.NextSyncAt.ShouldBeNull();
        (await ReloadAsync(other.Uid)).StatusCheckedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SyncWorkOrders_TwiceWithinCooldown_SecondIsNotDueAndSkipsUpstream()
    {
        await InsertSubmittedAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);

        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);
        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);

        _statusClient.WorkOrderRequests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SyncWorkOrders_AttemptOlderThanCooldown_IsDueAgain()
    {
        await InsertSubmittedAsync(statusCheckedAt: DateTimeOffset.UtcNow.AddMinutes(-6));
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);

        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);

        _statusClient.WorkOrderRequests.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("COMPLETED", 0)]
    [InlineData("completed", 0)]
    [InlineData("PERFORMED", 1)]
    public async Task SyncWorkOrders_CompletedIsSkipped(string category, int expectedRequests)
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasWorkOrderName = "UK-2026-2121";
        order.PythagorasStatusCategory = category;
        order.CompletedAt = DateTimeOffset.UtcNow.AddDays(-10);
        order.StatusCheckedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår", StatusCategory = "ONGOING" }]);

        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(expectedRequests == 0 ? WorkOrderRefreshOutcome.NotDue : WorkOrderRefreshOutcome.Refreshed);
        _statusClient.WorkOrderRequests.Count.ShouldBe(expectedRequests);
    }

    [Fact]
    public async Task SyncWorkOrder_Completed_IsNotDueAndSkipsUpstream()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasWorkOrderName = "UK-2026-2121";
        order.PythagorasStatusName = "Avslutad";
        order.PythagorasStatusCategory = "COMPLETED";
        order.CompletedAt = DateTimeOffset.UtcNow.AddDays(-10);
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);

        WorkOrderDetailModel result = await _handler.SyncWorkOrderAsync(order.Uid, "test@example.com");

        result.RefreshOutcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);
        result.Status.ShouldBe("Avslutad");
        _statusClient.WorkOrderRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SyncWorkOrder_ReadsOnlyThatOrder()
    {
        WorkOrderEntity target = await InsertSubmittedAsync();
        WorkOrderEntity sibling = await InsertSubmittedAsync();
        sibling.PythagorasWorkOrderId = 556;
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);

        (await _handler.SyncWorkOrderAsync(target.Uid, "test@example.com")).RefreshOutcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);

        _statusClient.WorkOrderRequests.Single().Parameters.ShouldBe("ids=555");
        (await ReloadAsync(sibling.Uid)).StatusCheckedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SyncWorkOrders_OrderMissingFromResponse_IsFailedKeepsStatusAndMovesStamp()
    {
        WorkOrderEntity missing = await InsertSubmittedAsync();
        missing.PythagorasStatusName = "Registrerad";
        missing.StatusCheckedAt = DateTimeOffset.UtcNow.AddDays(-1);
        WorkOrderEntity found = await InsertSubmittedAsync();
        found.PythagorasWorkOrderId = 556;
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 556, StatusId = 2, StatusName = "Pågår" }]);

        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(WorkOrderRefreshOutcome.Failed);
        WorkOrderEntity saved = await ReloadAsync(missing.Uid);
        saved.PythagorasStatusName.ShouldBe("Registrerad");
        saved.NextSyncAt.ShouldBeNull();
        saved.StatusCheckedAt!.Value.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        (await ReloadAsync(found.Uid)).PythagorasStatusName.ShouldBe("Pågår");
        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);
        _statusClient.WorkOrderRequests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SyncWorkOrders_ResponseWithoutStatus_IsFailedAndKeepsSavedStatus()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusName = "Registrerad";
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555 }]);

        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(WorkOrderRefreshOutcome.Failed);
        result.WorkOrders[0].Status.ShouldBe("Registrerad");
        (await ReloadAsync(order.Uid)).PythagorasStatusName.ShouldBe("Registrerad");
    }

    [Fact]
    public async Task SyncWorkOrders_UpstreamException_IsFailedAndConsumesCooldown()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusName = "Registrerad";
        await _dbContext.SaveChangesAsync();
        _statusClient.WorkOrderInfoException = new HttpRequestException("Unavailable");

        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(WorkOrderRefreshOutcome.Failed);
        result.WorkOrders[0].Status.ShouldBe("Registrerad");
        result.WorkOrders[0].StatusCheckedAt.ShouldNotBeNull();
        (await ReloadAsync(order.Uid)).StatusCheckedAt!.Value.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        // The failed attempt keeps the cooldown.
        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);
        _statusClient.WorkOrderRequests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SyncWorkOrders_CancelledMidRead_RethrowsAndKeepsSavedStatus()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusName = "Registrerad";
        await _dbContext.SaveChangesAsync();
        using CancellationTokenSource cts = new();
        _statusClient.CancelOnWorkOrderInfoRequest = cts;

        await Should.ThrowAsync<OperationCanceledException>(() => _handler.SyncWorkOrdersAsync("test@example.com", cts.Token));

        WorkOrderEntity saved = await ReloadAsync(order.Uid);
        saved.PythagorasStatusName.ShouldBe("Registrerad");
        // The only batch was the one in flight, so its claim is spent.
        saved.StatusCheckedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SyncWorkOrders_IgnoresNotifierDetailsInResponse()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.NotifierName = "Test User";
        order.NotifierEmail = "notifier@example.com";
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new()
        {
            Id = 555,
            StatusId = 2,
            StatusName = "Pågår",
            NotifierEmail = "ny.anmalare@example.com",
            NotifierName = "Ny Anmälare",
        }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        WorkOrderEntity saved = await ReloadAsync(order.Uid);
        saved.PythagorasStatusName.ShouldBe("Pågår");
        saved.NotifierEmail.ShouldBe("notifier@example.com");
        saved.NotifierName.ShouldBe("Test User");
    }

    [Fact]
    public async Task SyncWorkOrders_LeavesUpdatedAtAlone()
    {
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow.AddDays(-2);
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.UpdatedAt = updatedAt;
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);

        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);

        (await ReloadAsync(order.Uid)).UpdatedAt.ShouldBe(updatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SyncWorkOrders_StoresWorkOrderNumber()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, Name = "UK-2026-2121", StatusId = 2, StatusName = "Pågår" }]);

        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.WorkOrders[0].WorkOrderNumber.ShouldBe("UK-2026-2121");
        (await ReloadAsync(order.Uid)).PythagorasWorkOrderName.ShouldBe("UK-2026-2121");
    }

    [Fact]
    public async Task SyncWorkOrders_ResponseWithoutName_KeepsSavedWorkOrderNumber()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasWorkOrderName = "UK-2026-2121";
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        (await ReloadAsync(order.Uid)).PythagorasWorkOrderName.ShouldBe("UK-2026-2121");
    }

    [Fact]
    public async Task SyncWorkOrders_CompletedWithoutNumber_IsReadOnce()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusName = "Avslutad";
        order.PythagorasStatusCategory = "COMPLETED";
        order.CompletedAt = DateTimeOffset.UtcNow.AddDays(-10);
        order.StatusCheckedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, Name = "UK-2026-2121", StatusId = 3, StatusName = "Avslutad", StatusCategory = "COMPLETED" }]);

        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);
        // Past the cooldown again: only the number made it eligible, so it is not read twice.
        await _dbContext.WorkOrders.Where(e => e.Uid == order.Uid)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.StatusCheckedAt, DateTimeOffset.UtcNow.AddDays(-1)));
        _dbContext.ChangeTracker.Clear();

        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);

        _statusClient.WorkOrderRequests.Count.ShouldBe(1);
        (await ReloadAsync(order.Uid)).PythagorasWorkOrderName.ShouldBe("UK-2026-2121");
    }

    [Fact]
    public async Task SyncWorkOrders_CompletedWithoutNumberInResponse_IsReadOnceAndStoredEmpty()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusCategory = "COMPLETED";
        order.CompletedAt = DateTimeOffset.UtcNow.AddDays(-10);
        order.StatusCheckedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 3, StatusName = "Avslutad", StatusCategory = "COMPLETED" }]);

        WorkOrderRefreshModel first = await _handler.SyncWorkOrdersAsync("test@example.com");
        await _dbContext.WorkOrders.Where(e => e.Uid == order.Uid)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.StatusCheckedAt, DateTimeOffset.UtcNow.AddDays(-1)));
        _dbContext.ChangeTracker.Clear();

        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);

        _statusClient.WorkOrderRequests.Count.ShouldBe(1);
        first.WorkOrders[0].WorkOrderNumber.ShouldBeNull();
        (await ReloadAsync(order.Uid)).PythagorasWorkOrderName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task SyncWorkOrders_CompletedWithinWindow_IsReadAndStoresPerformedDescription()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasWorkOrderName = "UK-2026-2121";
        order.PythagorasStatusCategory = "COMPLETED";
        order.CompletedAt = DateTimeOffset.UtcNow.AddDays(-1);
        order.StatusCheckedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new()
        {
            Id = 555, StatusId = 3, StatusName = "Avslutad", StatusCategory = "COMPLETED",
            PerformedDescriptionDescription = "Gallret är bytt.", PerformedDescriptionCreated = 1788339433470
        }]);

        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);
        result.WorkOrders[0].PerformedDescription.ShouldBe("Gallret är bytt.");
        result.WorkOrders[0].PerformedDescriptionAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1788339433470));
        WorkOrderEntity saved = await ReloadAsync(order.Uid);
        saved.PerformedDescription.ShouldBe("Gallret är bytt.");
        saved.CompletedAt.ShouldBe(order.CompletedAt);
    }

    [Fact]
    public async Task SyncWorkOrders_BecomesCompleted_StampsCompletedAt()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 3, StatusName = "Avslutad", StatusCategory = "COMPLETED" }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        (await ReloadAsync(order.Uid)).CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SyncWorkOrders_Reopened_ClearsCompletedAtAndRemovedDescription()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasWorkOrderName = "UK-2026-2121";
        order.PythagorasStatusCategory = "COMPLETED";
        order.CompletedAt = DateTimeOffset.UtcNow.AddDays(-1);
        order.PerformedDescription = "Gallret är bytt.";
        order.PerformedDescriptionAt = DateTimeOffset.UtcNow.AddDays(-1);
        order.StatusCheckedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår", StatusCategory = "ONGOING" }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        WorkOrderEntity saved = await ReloadAsync(order.Uid);
        saved.CompletedAt.ShouldBeNull();
        saved.PerformedDescription.ShouldBeNull();
        saved.PerformedDescriptionAt.ShouldBeNull();
    }

    [Fact]
    public async Task SyncWorkOrders_StatusChanged_StampsPythagorasUpdated()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusId = 1;
        order.StatusChangedAt = DateTimeOffset.UtcNow.AddDays(-5);
        await _dbContext.SaveChangesAsync();
        DateTimeOffset updated = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds());
        _statusClient.SetWorkOrderInfoResults([new()
        {
            Id = 555, StatusId = 11, StatusName = "Beställt material", StatusCategory = "ONGOING",
            Updated = updated.ToUnixTimeMilliseconds()
        }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        (await ReloadAsync(order.Uid)).StatusChangedAt.ShouldBe(updated);
    }

    [Fact]
    public async Task SyncWorkOrders_StatusUnchanged_KeepsStatusChangedAt()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusId = 2;
        order.StatusChangedAt = DateTimeOffset.UtcNow.AddDays(-5);
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new()
        {
            Id = 555, StatusId = 2, StatusName = "Pågår", StatusCategory = "ONGOING",
            Updated = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds()
        }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        (await ReloadAsync(order.Uid)).StatusChangedAt.ShouldBe(order.StatusChangedAt);
    }

    [Fact]
    public async Task SyncWorkOrders_FirstStatusWithoutUpdated_StampsNow()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 1, StatusName = "Registrerad", StatusCategory = "NOT_STARTED" }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        (await ReloadAsync(order.Uid)).StatusChangedAt.ShouldNotBeNull().ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SyncWorkOrders_StatusChangedWithoutNewerUpdate_StampsNow()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusId = 1;
        order.StatusChangedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new()
        {
            Id = 555, StatusId = 2, StatusName = "Pågår", StatusCategory = "ONGOING",
            Updated = DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeMilliseconds()
        }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        (await ReloadAsync(order.Uid)).StatusChangedAt.ShouldNotBeNull().ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData("Gallret är bytt.", 0, 24, "saved")] // Unchanged: keeps the saved time.
    [InlineData("Gallret är bytt och målat.", 0, 24, "updated")] // Edited, same creation stamp: last update.
    [InlineData("Gallret är bytt och målat.", 1, 24, "created")] // Edited, newer creation stamp: that stamp.
    [InlineData("Gallret är bytt och målat.", 0, -24, "now")] // Edited, neither stamp moved: this read.
    public async Task SyncWorkOrders_PerformedDescription_IsDatedByItsLastChange(
        string description, int createdOffsetHours, int updatedOffsetHours, string expected)
    {
        DateTimeOffset savedAt = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeMilliseconds());
        DateTimeOffset createdAt = savedAt.AddHours(createdOffsetHours);
        DateTimeOffset updated = savedAt.AddHours(updatedOffsetHours);
        WorkOrderEntity order = await InsertSubmittedAsync();
        order.PythagorasStatusId = 2;
        order.PerformedDescription = "Gallret är bytt.";
        order.PerformedDescriptionAt = savedAt;
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([new()
        {
            Id = 555, StatusId = 2, StatusName = "Pågår", StatusCategory = "ONGOING",
            PerformedDescriptionDescription = description,
            PerformedDescriptionCreated = createdAt.ToUnixTimeMilliseconds(),
            Updated = updated.ToUnixTimeMilliseconds()
        }]);

        await _handler.SyncWorkOrdersAsync("test@example.com");

        WorkOrderEntity saved = await ReloadAsync(order.Uid);
        saved.PerformedDescription.ShouldBe(description);
        DateTimeOffset performedAt = saved.PerformedDescriptionAt.ShouldNotBeNull();
        switch (expected)
        {
            case "saved":
                performedAt.ShouldBe(savedAt);
                break;
            case "created":
                performedAt.ShouldBe(createdAt);
                break;
            case "updated":
                performedAt.ShouldBe(updated);
                break;
            default:
                performedAt.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
                break;
        }
    }

    [Fact]
    public async Task SyncWorkOrders_StatusChange_MovesOrderToTop()
    {
        WorkOrderEntity older = await InsertSubmittedAsync();
        older.CreatedAt = DateTimeOffset.UtcNow.AddDays(-10);
        older.PythagorasStatusId = 1;
        WorkOrderEntity newer = await InsertSubmittedAsync();
        newer.CreatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        newer.PythagorasWorkOrderId = 556;
        newer.PythagorasStatusId = 1;
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([
            new() { Id = 555, StatusId = 5, StatusName = "Vilande", StatusCategory = "ONGOING", Updated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
            new() { Id = 556, StatusId = 1, StatusName = "Registrerad", StatusCategory = "NOT_STARTED" }]);

        (await _handler.GetWorkOrdersAsync("test@example.com")).Select(e => e.Id).ShouldBe([newer.Uid, older.Uid]);
        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.WorkOrders.Select(e => e.Id).ShouldBe([older.Uid, newer.Uid]);
        (await _handler.GetWorkOrdersAsync("test@example.com")).Select(e => e.Id).ShouldBe([older.Uid, newer.Uid]);
    }

    [Fact]
    public async Task SyncWorkOrders_BatchesAtOneHundred_AndChecksUnchangedStatuses()
    {
        List<ServiceAccess.Pythagoras.Dto.WorkOrderInfoDto> infos = [];
        for (int i = 1; i <= 101; i++)
        {
            WorkOrderEntity order = await InsertSubmittedAsync();
            order.PythagorasWorkOrderId = i;
            order.PythagorasStatusName = "Pågår";
            infos.Add(new() { Id = i, StatusId = 2, StatusName = "Pågår" });
        }
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults(infos);

        WorkOrderRefreshModel result = await _handler.SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);
        _statusClient.WorkOrderRequests.Count.ShouldBe(2);
        result.WorkOrders.ShouldAllBe(e => e.StatusCheckedAt != null);
    }

    [Fact]
    public async Task SyncWorkOrders_CancelledOnFirstBatch_LeavesLaterBatchesUnclaimed()
    {
        for (int i = 1; i <= 101; i++)
        {
            WorkOrderEntity order = await InsertSubmittedAsync();
            order.PythagorasWorkOrderId = i;
        }
        await _dbContext.SaveChangesAsync();
        using CancellationTokenSource cts = new();
        _statusClient.CancelOnWorkOrderInfoRequest = cts;

        await Should.ThrowAsync<OperationCanceledException>(() => _handler.SyncWorkOrdersAsync("test@example.com", cts.Token));

        string requested = _statusClient.WorkOrderRequests.ShouldHaveSingleItem().Parameters!;
        HashSet<int> readIds = [.. requested["ids=".Length..].Split(',').Select(int.Parse)];
        readIds.Count.ShouldBe(100);
        _dbContext.ChangeTracker.Clear();
        List<WorkOrderEntity> saved = await _dbContext.WorkOrders.AsNoTracking().ToListAsync();

        // Only the batch that was in flight is burned; the rest were never claimed.
        saved.Where(e => readIds.Contains(e.PythagorasWorkOrderId!.Value)).ShouldAllBe(e => e.StatusCheckedAt != null);
        saved.Where(e => !readIds.Contains(e.PythagorasWorkOrderId!.Value)).ShouldAllBe(e => e.StatusCheckedAt == null);
        saved.Count(e => e.StatusCheckedAt == null).ShouldBe(1);
    }

    [Fact]
    public async Task SyncWorkOrders_BatchWrite_SavesEachOrdersOwnStatus()
    {
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow.AddDays(-2);
        WorkOrderEntity first = await InsertSubmittedAsync();
        WorkOrderEntity second = await InsertSubmittedAsync();
        second.PythagorasWorkOrderId = 556;
        first.UpdatedAt = updatedAt;
        second.UpdatedAt = updatedAt;
        await _dbContext.SaveChangesAsync();
        _statusClient.SetWorkOrderInfoResults([
            new() { Id = 555, Name = "UK-2026-1", StatusId = 2, StatusName = "Pågår", StatusCategory = "ONGOING" },
            new() { Id = 556, StatusId = 3, StatusName = "Avslutad", StatusCategory = "COMPLETED" }]);

        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);

        WorkOrderEntity savedFirst = await ReloadAsync(first.Uid);
        WorkOrderEntity savedSecond = await ReloadAsync(second.Uid);
        savedFirst.PythagorasWorkOrderName.ShouldBe("UK-2026-1");
        savedFirst.PythagorasStatusName.ShouldBe("Pågår");
        savedSecond.PythagorasWorkOrderName.ShouldBe(string.Empty);
        savedSecond.PythagorasStatusName.ShouldBe("Avslutad");
        savedSecond.PythagorasStatusCategory.ShouldBe("COMPLETED");
        savedFirst.UpdatedAt.ShouldBe(updatedAt, TimeSpan.FromSeconds(1));
        savedSecond.UpdatedAt.ShouldBe(updatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SyncWorkOrders_CooldownBelowFloor_IsFlooredAtSixtySeconds()
    {
        await InsertSubmittedAsync(statusCheckedAt: DateTimeOffset.UtcNow.AddSeconds(-45));
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);

        WorkOrderRefreshModel result = await CreateHandler(CreateTestConfig(cooldownSeconds: 30)).SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);
        _statusClient.WorkOrderRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SyncWorkOrders_CooldownAboveFloor_IsHonoured()
    {
        WorkOrderEntity recent = await InsertSubmittedAsync(statusCheckedAt: DateTimeOffset.UtcNow.AddSeconds(-120));
        _statusClient.SetWorkOrderInfoResults([new() { Id = 555, StatusId = 2, StatusName = "Pågår" }]);

        (await CreateHandler(CreateTestConfig(cooldownSeconds: 600)).SyncWorkOrdersAsync("test@example.com"))
            .Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);
        _statusClient.WorkOrderRequests.ShouldBeEmpty();

        await _dbContext.WorkOrders.Where(e => e.Uid == recent.Uid)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.StatusCheckedAt, DateTimeOffset.UtcNow.AddSeconds(-700)));
        _dbContext.ChangeTracker.Clear();

        (await CreateHandler(CreateTestConfig(cooldownSeconds: 600)).SyncWorkOrdersAsync("test@example.com"))
            .Outcome.ShouldBe(WorkOrderRefreshOutcome.Refreshed);
        _statusClient.WorkOrderRequests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SyncWorkOrders_RecentAndEmptyLists_DoNotCallPythagoras()
    {
        (await _handler.SyncWorkOrdersAsync("nobody@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);
        await InsertSubmittedAsync(statusCheckedAt: DateTimeOffset.UtcNow);

        (await _handler.SyncWorkOrdersAsync("test@example.com")).Outcome.ShouldBe(WorkOrderRefreshOutcome.NotDue);

        _statusClient.WorkOrderRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task SyncWorkOrders_StatusSyncDisabled_ReturnsDisabledWithoutUpstream()
    {
        await InsertSubmittedAsync();

        WorkOrderRefreshModel result = await CreateHandler(CreateTestConfig(statusSyncEnabled: false)).SyncWorkOrdersAsync("test@example.com");

        result.Outcome.ShouldBe(WorkOrderRefreshOutcome.Disabled);
        _statusClient.WorkOrderRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClaimForStatusRefresh_OverlappingClaim_ReturnsNothingForTheLoser()
    {
        WorkOrderEntity order = await InsertSubmittedAsync();
        WorkOrderRepository repository = new(_dbContext);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset staleBefore = now.AddMinutes(-1);

        IReadOnlyList<int> first = await repository.ClaimForStatusRefreshAsync([order.Id], staleBefore, now);
        IReadOnlyList<int> second = await repository.ClaimForStatusRefreshAsync([order.Id], staleBefore, now.AddSeconds(1));

        first.ShouldHaveSingleItem().ShouldBe(order.Id);
        second.ShouldBeEmpty();
    }

    private WorkOrderHandler CreateHandler(ApplicationConfig? config = null, WorkOrderConfiguration? accessConfig = null)
    {
        config ??= CreateTestConfig();
        return new WorkOrderHandler(
            new WorkOrderRepository(_dbContext),
            _dataStore,
            new WorkOrderChannel(),
            new LocalWorkOrderFileStorage(config),
            new WorkOrderFileValidator(config),
            new WorkOrderCategoryProvider(_dataStore),
            new WorkOrderAccessPolicy(accessConfig ?? new WorkOrderConfiguration()),
            new WorkOrderStatusSyncService(new WorkOrderRepository(_dbContext), _statusClient, _dataStore, config,
                NullLogger<WorkOrderStatusSyncService>.Instance),
            NullLogger<WorkOrderHandler>.Instance);
    }

    // Defaults mirror a freshly submitted order: no submission schedule and no status read yet.
    private async Task<WorkOrderEntity> InsertSubmittedAsync(
        DateTimeOffset? statusCheckedAt = null, DateTimeOffset? nextSyncAt = null)
    {
        WorkOrderEntity entity = await InsertWorkOrderAsync(WorkOrderSyncStatus.Submitted, nextSyncAt, description: "Submitted order");
        entity.PythagorasWorkOrderId = 555;
        entity.StatusCheckedAt = statusCheckedAt;
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    private async Task<WorkOrderEntity> ReloadAsync(Guid uid) =>
        (await _dbContext.WorkOrders.AsNoTracking().FirstOrDefaultAsync(w => w.Uid == uid))!;

    private static ApplicationConfig CreateTestConfig(bool statusSyncEnabled = true, int cooldownSeconds = 300)
    {
        Dictionary<string, string?> configData = new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Test",
            ["WorkOrder:FileStorage"] = Path.Combine(Path.GetTempPath(), "workOrder-handler-tests"),
            ["WorkOrder:MaxRetries"] = "3",
            ["WorkOrder:StatusSyncEnabled"] = statusSyncEnabled ? "true" : "false",
            ["WorkOrder:StatusRefreshCooldownSeconds"] = cooldownSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
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
