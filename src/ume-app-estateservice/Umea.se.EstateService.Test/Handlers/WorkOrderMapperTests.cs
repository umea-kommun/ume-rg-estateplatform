using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Handlers;

public class WorkOrderMapperTests
{
    [Theory]
    [InlineData("NOT_STARTED", WorkOrderDisplayStatus.Received)]
    [InlineData("ONGOING", WorkOrderDisplayStatus.InProgress)]
    [InlineData("ongoing", WorkOrderDisplayStatus.InProgress)]
    [InlineData("PERFORMED", WorkOrderDisplayStatus.Closed)]
    [InlineData("COMPLETED", WorkOrderDisplayStatus.Closed)]
    public void MapDisplayStatus_MapsCategory(string category, WorkOrderDisplayStatus expected)
    {
        WorkOrderMapper.MapDisplayStatus("Tilldelad", category).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Vilande", WorkOrderDisplayStatus.OnHold)]
    [InlineData("Beställt material", WorkOrderDisplayStatus.MaterialOrdered)]
    [InlineData("Skickad till entreprenör", WorkOrderDisplayStatus.SentToContractor)]
    [InlineData("  skickad TILL entreprenör ", WorkOrderDisplayStatus.SentToContractor)]
    public void MapDisplayStatus_NamedStatus_WinsOverCategory(string name, WorkOrderDisplayStatus expected)
    {
        WorkOrderMapper.MapDisplayStatus(name, "ONGOING").ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("SOMETHING_NEW")]
    public void MapDisplayStatus_UnknownOrMissingCategory_IsNull(string? category)
    {
        WorkOrderMapper.MapDisplayStatus(null, category).ShouldBeNull();
    }

    [Fact]
    public void MapToListItems_SortsByAndReturnsLatestChange()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WorkOrderEntity created = new() { Uid = Guid.NewGuid(), CreatedAt = now.AddDays(-1) };
        WorkOrderEntity statusChanged = new() { Uid = Guid.NewGuid(), CreatedAt = now.AddDays(-9), StatusChangedAt = now };
        WorkOrderEntity performed = new()
        {
            Uid = Guid.NewGuid(),
            CreatedAt = now.AddDays(-8),
            StatusChangedAt = now.AddDays(-7),
            PerformedDescriptionAt = now.AddHours(-1),
        };
        WorkOrderEntity untouched = new() { Uid = Guid.NewGuid(), CreatedAt = now.AddDays(-2) };

        List<WorkOrderListItemModel> items = WorkOrderMapper.MapToListItems(
            [untouched, performed, created, statusChanged], new Dictionary<int, BuildingEntity>());

        items.Select(e => e.Id).ShouldBe([statusChanged.Uid, performed.Uid, created.Uid, untouched.Uid]);
        items.Select(e => e.LastChangedAt).ShouldBe([now, now.AddHours(-1), now.AddDays(-1), now.AddDays(-2)]);
    }

    [Fact]
    public void MapToListItems_DismissedUsesUpdatedAt_OthersIgnoreIt()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WorkOrderEntity dismissed = new()
        {
            Uid = Guid.NewGuid(),
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now,
            SyncStatus = WorkOrderSyncStatus.Dismissed,
        };
        WorkOrderEntity submitted = new()
        {
            Uid = Guid.NewGuid(),
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddHours(1),
            SyncStatus = WorkOrderSyncStatus.Submitted,
        };

        List<WorkOrderListItemModel> items = WorkOrderMapper.MapToListItems(
            [submitted, dismissed], new Dictionary<int, BuildingEntity>());

        items.Select(e => e.Id).ShouldBe([dismissed.Uid, submitted.Uid]);
        items.Select(e => e.LastChangedAt).ShouldBe([now, now.AddDays(-1)]);
    }
}
