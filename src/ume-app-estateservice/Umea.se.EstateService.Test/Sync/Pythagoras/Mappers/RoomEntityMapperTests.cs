using Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Test.Sync.Pythagoras.Mappers;

public class RoomEntityMapperTests
{
    [Theory]
    [InlineData("R01")]
    [InlineData("r01")]
    [InlineData("unset")]
    [InlineData("UnSeT")]
    public void ToEntities_PlaceholderWorkspace_IsSkipped(string name)
    {
        Workspace[] dtos =
        [
            new() { Id = 1, Name = name, BuildingId = 10, FloorId = 100 },
            new() { Id = 2, Name = "1001", BuildingId = 10, FloorId = 100 }
        ];

        List<RoomEntity> rooms = RoomEntityMapper.ToEntities(dtos);

        rooms.Select(room => room.Id).ShouldBe([2]);
    }

    [Theory]
    [InlineData("R1")]
    [InlineData("R011")]
    [InlineData("1001")]
    public void ToEntities_RealWorkspace_IsKept(string name)
    {
        List<RoomEntity> rooms = RoomEntityMapper.ToEntities([new Workspace { Id = 1, Name = name }]);

        rooms.ShouldHaveSingleItem().Name.ShouldBe(name);
    }

    [Fact]
    public void ToEntities_OnlyPlaceholders_ReturnsEmpty()
    {
        List<RoomEntity> rooms = RoomEntityMapper.ToEntities(
        [
            new Workspace { Id = 1, Name = "R01" },
            new Workspace { Id = 2, Name = "unset" }
        ]);

        rooms.ShouldBeEmpty();
    }
}
