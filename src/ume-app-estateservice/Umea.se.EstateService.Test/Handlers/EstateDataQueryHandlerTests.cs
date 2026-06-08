using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Handlers;

public class EstateDataQueryHandlerTests
{
    private readonly EstateDataQueryHandler _handler;

    private static readonly RoomEntity Room100 = new() { Id = 100, Name = "Room A", PopularName = "RA", BuildingId = 10, FloorId = 200 };
    private static readonly RoomEntity Room101 = new() { Id = 101, Name = "Room B", PopularName = "RB", BuildingId = 10, FloorId = 200 };
    private static readonly RoomEntity Room102 = new() { Id = 102, Name = "Garage room", PopularName = "GR", BuildingId = 11, FloorId = null };
    private static readonly FloorEntity Floor200 = new() { Id = 200, Name = "Floor 1", PopularName = "F1", BuildingId = 10 };

    private static readonly BuildingEntity Building10 = new()
    {
        Id = 10,
        Name = "Library",
        PopularName = "Central",
        PropertyDesignation = "Räven 1",
        EstateId = 1,
        NumRooms = 2,
        NumFloors = 1,
        GeoLocation = new GeoPointModel(63.8, 20.2),
        ImageIds = [1],
        BusinessType = new BusinessTypeModel { Id = 5, Name = "Skola" },
        Floors = [Floor200],
        Rooms = [Room100, Room101]
    };

    private static readonly BuildingEntity Building11 = new()
    {
        Id = 11,
        Name = "Garage",
        PopularName = "G",
        EstateId = 2,
        BusinessType = new BusinessTypeModel { Id = 5, Name = "Skola" },
        Rooms = [Room102]
    };

    private static readonly BuildingEntity Building12 = new()
    {
        Id = 12,
        Name = "Office",
        PopularName = "O",
        EstateId = 1,
        ImageIds = [],
        BusinessType = new BusinessTypeModel { Id = 3, Name = "Kontor" }
    };

    private static readonly EstateEntity Estate1 = new() { Id = 1, Name = "Estate One", PopularName = "E1" };
    private static readonly EstateEntity Estate2 = new() { Id = 2, Name = "Estate Two", PopularName = "E2" };

    public EstateDataQueryHandlerTests()
    {
        InMemoryDataStore dataStore = new();
        DataStoreSeeder.Seed(
            dataStore,
            estates: [Estate1, Estate2],
            buildings: [Building10, Building11, Building12],
            floors: [Floor200],
            rooms: [Room100, Room101, Room102],
            buildingAscendants: new Dictionary<int, BuildingAscendantTriplet>
            {
                [10] = new BuildingAscendantTriplet
                {
                    Estate = new BuildingAscendantModel { Id = 1, Name = "Estate One" }
                }
            });

        _handler = new EstateDataQueryHandler(dataStore);
    }

    [Fact]
    public async Task GetBuildingsAsync_NoFilters_ReturnsAll()
    {
        IReadOnlyList<BuildingInfoModel> result = await _handler.GetBuildingsAsync();

        result.Select(b => b.Id).ShouldBe([10, 11, 12], ignoreOrder: true);
    }

    [Fact]
    public async Task GetBuildingsAsync_FilterByIds_ReturnsMatching()
    {
        IReadOnlyList<BuildingInfoModel> result = await _handler.GetBuildingsAsync(buildingIds: [10, 12]);

        result.Select(b => b.Id).ShouldBe([10, 12], ignoreOrder: true);
    }

    [Fact]
    public async Task GetBuildingsAsync_FilterByEstateId_ReturnsMatching()
    {
        IReadOnlyList<BuildingInfoModel> result = await _handler.GetBuildingsAsync(estateId: 1);

        result.Select(b => b.Id).ShouldBe([10, 12], ignoreOrder: true);
    }

    [Fact]
    public async Task GetBuildingsAsync_SearchByPropertyDesignation_ReturnsMatching()
    {
        IReadOnlyList<BuildingInfoModel> result = await _handler.GetBuildingsAsync(queryArgs: QueryArgs.WithSearch("räven"));

        result.Select(b => b.Id).ShouldBe([10]);
    }

    [Fact]
    public async Task GetBuildingsAsync_Paging_AppliesSkipAndTake()
    {
        IReadOnlyList<BuildingInfoModel> result = await _handler.GetBuildingsAsync(queryArgs: QueryArgs.WithPaging(skip: 1, take: 1));

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(11);
    }

    [Fact]
    public async Task GetBuildingsAsync_NegativeSkip_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(
            async () => await _handler.GetBuildingsAsync(queryArgs: QueryArgs.WithPaging(skip: -1)));
    }

    [Fact]
    public async Task GetBuildingByIdAsync_Existing_ReturnsBuilding()
    {
        BuildingInfoModel result = await _handler.GetBuildingByIdAsync(10);

        result.Id.ShouldBe(10);
    }

    [Fact]
    public async Task GetBuildingByIdAsync_Missing_ThrowsEntityNotFound()
    {
        await Should.ThrowAsync<EntityNotFoundException>(
            async () => await _handler.GetBuildingByIdAsync(999));
    }

    [Fact]
    public async Task GetEstateByIdAsync_Existing_ReturnsEstate()
    {
        EstateModel result = await _handler.GetEstateByIdAsync(1);

        result.Id.ShouldBe(1);
    }

    [Fact]
    public async Task GetEstateByIdAsync_Missing_ThrowsEntityNotFound()
    {
        await Should.ThrowAsync<EntityNotFoundException>(
            async () => await _handler.GetEstateByIdAsync(999));
    }

    [Fact]
    public async Task GetEstatesWithBuildingsAsync_ReturnsAllEstates()
    {
        IReadOnlyList<EstateModel> result = await _handler.GetEstatesWithBuildingsAsync();

        result.Select(e => e.Id).ShouldBe([1, 2], ignoreOrder: true);
    }

    [Fact]
    public async Task GetRoomsAsync_FilterByRoomIds_ReturnsMatching()
    {
        IReadOnlyList<RoomModel> result = await _handler.GetRoomsAsync(roomIds: [100, 102]);

        result.Select(r => r.Id).ShouldBe([100, 102], ignoreOrder: true);
    }

    [Fact]
    public async Task GetRoomsAsync_FilterByBuildingAndFloor_ReturnsMatching()
    {
        IReadOnlyList<RoomModel> result = await _handler.GetRoomsAsync(buildingId: 10, floorId: 200);

        result.Select(r => r.Id).ShouldBe([100, 101], ignoreOrder: true);
    }

    [Fact]
    public async Task GetBuildingWorkspacesAsync_FilterByFloor_ReturnsRooms()
    {
        IReadOnlyList<RoomModel> result = await _handler.GetBuildingWorkspacesAsync(buildingId: 10, floorId: 200);

        result.Select(r => r.Id).ShouldBe([100, 101], ignoreOrder: true);
    }

    [Fact]
    public async Task GetBuildingWorkspacesAsync_UnknownBuilding_ReturnsEmpty()
    {
        IReadOnlyList<RoomModel> result = await _handler.GetBuildingWorkspacesAsync(buildingId: 999);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_NoFloors_ReturnsEmpty()
    {
        IReadOnlyList<FloorInfoModel> result = await _handler.GetBuildingFloorsAsync(buildingId: 11);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_ExcludeRooms_ReturnsFloorsWithoutRooms()
    {
        IReadOnlyList<FloorInfoModel> result = await _handler.GetBuildingFloorsAsync(buildingId: 10, includeRooms: false);

        result.Count.ShouldBe(1);
        result[0].Rooms.ShouldBeNull();
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_IncludeRooms_GroupsRoomsByFloor()
    {
        IReadOnlyList<FloorInfoModel> result = await _handler.GetBuildingFloorsAsync(buildingId: 10, includeRooms: true);

        result.Count.ShouldBe(1);
        result[0].Rooms.ShouldNotBeNull();
        result[0].Rooms!.Select(r => r.Id).ShouldBe([100, 101], ignoreOrder: true);
    }

    [Fact]
    public async Task GetFloorsAsync_FilterByIds_ReturnsMatching()
    {
        IReadOnlyList<FloorInfoModel> result = await _handler.GetFloorsAsync(floorIds: [200]);

        result.Select(f => f.Id).ShouldBe([200]);
    }

    [Fact]
    public async Task GetBuildingWorkspaceStatsAsync_ReturnsCountsPerBuilding()
    {
        IReadOnlyDictionary<int, BuildingWorkspaceStatsModel> result = await _handler.GetBuildingWorkspaceStatsAsync();

        result[10].NumberOfRooms.ShouldBe(2);
        result[10].NumberOfFloors.ShouldBe(1);
    }

    [Fact]
    public async Task GetBusinessTypesAsync_ReturnsDistinctOrderedByName()
    {
        IReadOnlyList<BusinessTypeModel> result = await _handler.GetBusinessTypesAsync();

        result.Select(bt => bt.Id).ShouldBe([3, 5]);
    }

    [Fact]
    public async Task GetBuildingGeolocationsAsync_ReturnsOnlyBuildingsWithGeo()
    {
        IReadOnlyList<BuildingLocationModel> result = await _handler.GetBuildingGeolocationsAsync();

        result.Select(b => b.Id).ShouldBe([10]);
    }

    [Fact]
    public void StampBuildingImageUrls_BuildingWithImages_SetsImageUrl()
    {
        PythagorasDocument doc = new() { Id = 10, Type = NodeType.Building, Name = "Library" };

        _handler.StampBuildingImageUrls([doc]);

        doc.ImageUrl.ShouldBe("/api/buildings/10/image");
    }

    [Fact]
    public void StampBuildingImageUrls_BuildingWithEmptyImageIds_LeavesImageUrlNull()
    {
        PythagorasDocument doc = new() { Id = 12, Type = NodeType.Building, Name = "Office" };

        _handler.StampBuildingImageUrls([doc]);

        doc.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public void StampBuildingImageUrls_NonBuildingDocument_IsSkipped()
    {
        PythagorasDocument doc = new() { Id = 10, Type = NodeType.Room, Name = "Room A" };

        _handler.StampBuildingImageUrls([doc]);

        doc.ImageUrl.ShouldBeNull();
    }
}
