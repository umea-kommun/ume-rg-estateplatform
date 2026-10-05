using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Handlers;

public class EstateModelMapperTests
{
    private static BuildingEntity CreateBuilding(BuildingNoticeBoardModel? noticeBoard = null) =>
        new()
        {
            Id = 1,
            Name = "B1",
            PopularName = "B1",
            NoticeBoard = noticeBoard
        };

    [Fact]
    public void MapBuildingInfo_NoNoticeBoard_ReturnsNull()
    {
        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(CreateBuilding());

        result.ExtendedProperties!.NoticeBoard.ShouldBeNull();
    }

    [Fact]
    public void MapBuildingInfo_NoDates_ReturnsNoticeBoard()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel { Text = "Hello" });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldNotBeNull();
        result.ExtendedProperties!.NoticeBoard!.Text.ShouldBe("Hello");
    }

    [Fact]
    public void MapBuildingInfo_ExpiredEndDate_ReturnsNull()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "Expired",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31)
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldBeNull();
    }

    [Fact]
    public void MapBuildingInfo_FutureStartDate_ReturnsNull()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "Future",
            StartDate = DateTime.Today.AddDays(30),
            EndDate = DateTime.Today.AddDays(60)
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldBeNull();
    }

    [Fact]
    public void MapBuildingInfo_CurrentlyActive_ReturnsNoticeBoard()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "Active",
            StartDate = DateTime.Today.AddDays(-10),
            EndDate = DateTime.Today.AddDays(10)
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldNotBeNull();
        result.ExtendedProperties!.NoticeBoard!.Text.ShouldBe("Active");
    }

    [Fact]
    public void MapBuildingInfo_EndDateToday_ReturnsNoticeBoard()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "Last day",
            EndDate = DateTime.Today
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldNotBeNull();
    }

    [Fact]
    public void MapBuildingInfo_StartDateToday_ReturnsNoticeBoard()
    {
        BuildingEntity building = CreateBuilding(new BuildingNoticeBoardModel
        {
            Text = "First day",
            StartDate = DateTime.Today
        });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.ExtendedProperties!.NoticeBoard.ShouldNotBeNull();
    }

    [Fact]
    public void MapBuildingInfo_PlaceholderFloorAndRoom_HasRoomInformationIsFalse()
    {
        BuildingEntity building = CreateBuilding();
        building.Floors.Add(new FloorEntity { Id = 10, Name = "VF01", BuildingId = building.Id });
        building.Rooms.Add(new RoomEntity { Id = 100, Name = "R01", BuildingId = building.Id, FloorId = 10 });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.HasRoomInformation.ShouldBeFalse();
    }

    [Fact]
    public void MapBuildingInfo_NoFloorsOrRooms_HasRoomInformationIsFalse()
    {
        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(CreateBuilding());

        result.HasRoomInformation.ShouldBeFalse();
    }

    [Fact]
    public void MapBuildingInfo_FloorsButNoRooms_HasRoomInformationIsFalse()
    {
        BuildingEntity building = CreateBuilding();
        building.Floors.Add(new FloorEntity { Id = 10, Name = "Plan 1", BuildingId = building.Id });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.HasRoomInformation.ShouldBeFalse();
    }

    [Fact]
    public void MapBuildingInfo_RoomsButNoFloors_HasRoomInformationIsFalse()
    {
        BuildingEntity building = CreateBuilding();
        building.Rooms.Add(new RoomEntity { Id = 100, Name = "101", BuildingId = building.Id, FloorId = 10 });
        building.Rooms.Add(new RoomEntity { Id = 101, Name = "102", BuildingId = building.Id, FloorId = 10 });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.HasRoomInformation.ShouldBeFalse();
    }

    [Fact]
    public void MapBuildingInfo_PlaceholderFloorWithSingleNamedRoom_HasRoomInformationIsFalse()
    {
        BuildingEntity building = CreateBuilding();
        building.Floors.Add(new FloorEntity { Id = 10, Name = "VF01", BuildingId = building.Id });
        building.Rooms.Add(new RoomEntity { Id = 100, Name = "101", BuildingId = building.Id, FloorId = 10 });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.HasRoomInformation.ShouldBeFalse();
    }

    [Fact]
    public void MapBuildingInfo_RealRooms_HasRoomInformationIsTrue()
    {
        BuildingEntity building = CreateBuilding();
        building.Floors.Add(new FloorEntity { Id = 10, Name = "Plan 1", BuildingId = building.Id });
        building.Rooms.Add(new RoomEntity { Id = 100, Name = "101", BuildingId = building.Id, FloorId = 10 });
        building.Rooms.Add(new RoomEntity { Id = 101, Name = "102", BuildingId = building.Id, FloorId = 10 });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.HasRoomInformation.ShouldBeTrue();
    }

    [Fact]
    public void MapBuildingInfo_PlaceholderFloorButExtraRoom_HasRoomInformationIsTrue()
    {
        BuildingEntity building = CreateBuilding();
        building.Floors.Add(new FloorEntity { Id = 10, Name = "VF01", BuildingId = building.Id });
        building.Rooms.Add(new RoomEntity { Id = 100, Name = "R01", BuildingId = building.Id, FloorId = 10 });
        building.Rooms.Add(new RoomEntity { Id = 101, Name = "R02", BuildingId = building.Id, FloorId = 10 });

        BuildingInfoModel result = EstateModelMapper.MapBuildingInfo(building);

        result.HasRoomInformation.ShouldBeTrue();
    }
}
