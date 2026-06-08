using Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.Sync.Pythagoras.Mappers;

public class BuildingEntityMapperTests
{
    private static Dictionary<int, PropertyValueDto> Props(params (PropertyCategoryId Id, string? Value)[] values)
        => values.ToDictionary(v => (int)v.Id, v => new PropertyValueDto { Value = v.Value });

    [Fact]
    public void ToEntity_NullDto_Throws()
    {
        Should.Throw<ArgumentNullException>(() => BuildingEntityMapper.ToEntity(null!));
    }

    [Fact]
    public void ToEntity_MapsCoreScalarFields()
    {
        Guid uid = Guid.NewGuid();
        BuildingInfo dto = new()
        {
            Id = 42,
            Uid = uid,
            Name = "Stadshuset",
            PopularName = "City Hall",
            Grossarea = 1200.5m,
            Netarea = 980.25m
        };

        BuildingEntity entity = BuildingEntityMapper.ToEntity(dto, estateId: 7);

        entity.Id.ShouldBe(42);
        entity.Uid.ShouldBe(uid);
        entity.Name.ShouldBe("Stadshuset");
        entity.PopularName.ShouldBe("City Hall");
        entity.GrossArea.ShouldBe(1200.5m);
        entity.NetArea.ShouldBe(980.25m);
        entity.EstateId.ShouldBe(7);
        entity.Floors.ShouldBeEmpty();
        entity.Rooms.ShouldBeEmpty();
    }

    [Fact]
    public void ToEntity_DefaultEstateId_IsZero()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo { Id = 1 });

        entity.EstateId.ShouldBe(0);
    }

    [Fact]
    public void ToEntity_NullAreas_DefaultToZero()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo { Id = 1, Grossarea = null, Netarea = null });

        entity.GrossArea.ShouldBe(0m);
        entity.NetArea.ShouldBe(0m);
    }

    [Fact]
    public void ToEntity_SetsUpdatedAtToRecentUtc()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-5);

        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo { Id = 1 });

        entity.UpdatedAt.ShouldBeInRange(before, DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void ToEntity_BothCoordinatesZero_GeoLocationNull()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo { Id = 1, GeoX = 0, GeoY = 0 });

        entity.GeoLocation.ShouldBeNull();
    }

    [Theory]
    [InlineData(63.8258, 20.2630)]
    [InlineData(0, 20.2630)]
    [InlineData(63.8258, 0)]
    public void ToEntity_AnyNonZeroCoordinate_MapsGeoLocation(double geoX, double geoY)
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo { Id = 1, GeoX = geoX, GeoY = geoY });

        entity.GeoLocation.ShouldNotBeNull();
        entity.GeoLocation!.Lat.ShouldBe(geoX);
        entity.GeoLocation.Lon.ShouldBe(geoY);
    }

    [Fact]
    public void ToEntity_NoAddressFields_AddressNull()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo { Id = 1 });

        entity.Address.ShouldBeNull();
    }

    [Theory]
    [InlineData("Skolgatan 31A", "", "", "", "")]
    [InlineData("", "901 84", "", "", "")]
    [InlineData("", "", "Umeå", "", "")]
    [InlineData("", "", "", "Sverige", "")]
    [InlineData("", "", "", "", "Plan 2")]
    public void ToEntity_AnyAddressFieldPresent_MapsAddress(string street, string zip, string city, string country, string extra)
    {
        BuildingInfo dto = new()
        {
            Id = 1,
            AddressStreet = street,
            AddressZipCode = zip,
            AddressCity = city,
            AddressCountry = country,
            AddressExtra = extra
        };

        BuildingEntity entity = BuildingEntityMapper.ToEntity(dto);

        entity.Address.ShouldNotBeNull();
        entity.Address!.Street.ShouldBe(street);
        entity.Address.ZipCode.ShouldBe(zip);
        entity.Address.City.ShouldBe(city);
        entity.Address.Country.ShouldBe(country);
        entity.Address.Extra.ShouldBe(extra);
    }

    [Fact]
    public void ToEntity_MapsPropertyValueBackedScalars()
    {
        BuildingInfo dto = new()
        {
            Id = 1,
            PropertyValues = Props(
                (PropertyCategoryId.YearOfConstruction, "1973"),
                (PropertyCategoryId.BuildingCondition, "Good"),
                (PropertyCategoryId.BuildingExternalOwnerName, "Acme AB"),
                (PropertyCategoryId.BuildingExternalOwnerNote, "Leased until 2030"),
                (PropertyCategoryId.PropertyDesignation, "Kvarteret Räven 1"))
        };

        BuildingEntity entity = BuildingEntityMapper.ToEntity(dto);

        entity.YearOfConstruction.ShouldBe("1973");
        entity.BuildingCondition.ShouldBe("Good");
        entity.ExternalOwnerName.ShouldBe("Acme AB");
        entity.ExternalOwnerNote.ShouldBe("Leased until 2030");
        entity.PropertyDesignation.ShouldBe("Kvarteret Räven 1");
    }

    [Theory]
    [InlineData("Ja", true)]
    [InlineData("Nej", false)]
    [InlineData("Kanske", null)]
    [InlineData(null, null)]
    public void ToEntity_BlueprintAvailable_ParsesKnownValues(string? raw, bool? expected)
    {
        BuildingInfo dto = new()
        {
            Id = 1,
            PropertyValues = raw is null ? [] : Props((PropertyCategoryId.BlueprintAvailable, raw))
        };

        BuildingEntity entity = BuildingEntityMapper.ToEntity(dto);

        entity.BlueprintAvailable.ShouldBe(expected);
    }

    [Fact]
    public void ToEntity_NoNoticeBoardText_NoticeBoardNull()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props((PropertyCategoryId.NoticeBoardText, "   "))
        });

        entity.NoticeBoard.ShouldBeNull();
    }

    [Fact]
    public void ToEntity_NoticeBoardTextOnly_DatesNull()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props((PropertyCategoryId.NoticeBoardText, "Elevator service"))
        });

        entity.NoticeBoard.ShouldNotBeNull();
        entity.NoticeBoard!.Text.ShouldBe("Elevator service");
        entity.NoticeBoard.StartDate.ShouldBeNull();
        entity.NoticeBoard.EndDate.ShouldBeNull();
    }

    [Fact]
    public void ToEntity_NoticeBoardWithValidDates_ParsesDates()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props(
                (PropertyCategoryId.NoticeBoardText, "Renovation"),
                (PropertyCategoryId.NoticeBoardStartDate, "2026-01-15"),
                (PropertyCategoryId.NoticeBoardEndDate, "2026-02-28"))
        });

        entity.NoticeBoard.ShouldNotBeNull();
        entity.NoticeBoard!.StartDate.ShouldBe(new DateTime(2026, 1, 15));
        entity.NoticeBoard.EndDate.ShouldBe(new DateTime(2026, 2, 28));
    }

    [Fact]
    public void ToEntity_NoticeBoardWithUnparseableDates_DatesNull()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props(
                (PropertyCategoryId.NoticeBoardText, "Notice"),
                (PropertyCategoryId.NoticeBoardStartDate, "not-a-date"),
                (PropertyCategoryId.NoticeBoardEndDate, "also-bad"))
        });

        entity.NoticeBoard.ShouldNotBeNull();
        entity.NoticeBoard!.StartDate.ShouldBeNull();
        entity.NoticeBoard.EndDate.ShouldBeNull();
    }

    [Fact]
    public void ToEntity_NoContactProperties_ContactPersonsNull()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo { Id = 1 });

        entity.ContactPersons.ShouldBeNull();
        entity.LegacyPropertyManager.ShouldBe(string.Empty);
        entity.LegacyOperationsManager.ShouldBeNull();
    }

    [Fact]
    public void ToEntity_PropertyManagerWithContact_ParsesPhoneAndEmail()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props(
                (PropertyCategoryId.PropertyManager, "Anna Andersson"),
                (PropertyCategoryId.PropertyManagerContact, "090-123456 / anna@umea.se"))
        });

        entity.ContactPersons.ShouldNotBeNull();
        entity.ContactPersons!.PropertyManager.Name.ShouldBe("Anna Andersson");
        entity.ContactPersons.PropertyManager.Phone.ShouldBe("090-123456");
        entity.ContactPersons.PropertyManager.Email.ShouldBe("anna@umea.se");
        entity.LegacyPropertyManager.ShouldBe("Anna Andersson");
    }

    [Fact]
    public void ToEntity_OnlyNonManagerRolePopulated_SynthesizesEmptyPropertyManager()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props((PropertyCategoryId.OperationsManager, "Bertil Berg"))
        });

        entity.ContactPersons.ShouldNotBeNull();
        entity.ContactPersons!.PropertyManager.Name.ShouldBe(string.Empty);
        entity.ContactPersons.OperationsManager.ShouldNotBeNull();
        entity.ContactPersons.OperationsManager!.Name.ShouldBe("Bertil Berg");
        entity.LegacyPropertyManager.ShouldBe(string.Empty);
        entity.LegacyOperationsManager.ShouldBe("Bertil Berg");
    }

    [Fact]
    public void ToEntity_NoExternalStatus_WorkOrderTypesHaveBaseSet()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo { Id = 1 });

        entity.WorkOrderTypes.ShouldBe([WorkOrderType.ErrorReport, WorkOrderType.BuildingService, WorkOrderType.SpaceRequirement]);
    }

    [Fact]
    public void ToEntity_ExternalStatusEgen_KeepsBaseWorkOrderTypes()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props((PropertyCategoryId.BuildingExternalStatus, "Egen"))
        });

        entity.WorkOrderTypes.ShouldContain(WorkOrderType.ErrorReport);
        entity.WorkOrderTypes.ShouldContain(WorkOrderType.BuildingService);
    }

    [Fact]
    public void ToEntity_ExternalStatusNotEgen_WorkOrderTypesEmpty()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props((PropertyCategoryId.BuildingExternalStatus, "Extern"))
        });

        entity.WorkOrderTypes.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(PropertyCategoryId.TownHallServiceOrder, WorkOrderType.TownHallService)]
    [InlineData(PropertyCategoryId.FacilityServiceOrder, WorkOrderType.FacilityService)]
    public void ToEntity_ServiceOrderOptIn_AddsWorkOrderType(PropertyCategoryId optIn, WorkOrderType expected)
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            PropertyValues = Props((optIn, "Ja"))
        });

        entity.WorkOrderTypes.ShouldContain(expected);
    }

    [Fact]
    public void ToEntity_BusinessTypeIdAndName_MapsBusinessType()
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            BusinessTypeId = 5,
            BusinessTypeName = "Skola"
        });

        entity.BusinessType.ShouldNotBeNull();
        entity.BusinessType!.Id.ShouldBe(5);
        entity.BusinessType.Name.ShouldBe("Skola");
    }

    [Theory]
    [InlineData(null, "Skola")]
    [InlineData(5, null)]
    [InlineData(5, "")]
    [InlineData(5, "   ")]
    public void ToEntity_IncompleteBusinessType_BusinessTypeNull(int? id, string? name)
    {
        BuildingEntity entity = BuildingEntityMapper.ToEntity(new BuildingInfo
        {
            Id = 1,
            BusinessTypeId = id,
            BusinessTypeName = name
        });

        entity.BusinessType.ShouldBeNull();
    }

    [Fact]
    public void ToEntities_EmptyList_ReturnsEmpty()
    {
        BuildingEntityMapper.ToEntities([]).ShouldBeEmpty();
    }

    [Fact]
    public void ToEntities_MapsEachDto()
    {
        List<BuildingEntity> entities = BuildingEntityMapper.ToEntities(
        [
            new BuildingInfo { Id = 1, Name = "A" },
            new BuildingInfo { Id = 2, Name = "B" }
        ]);

        entities.Count.ShouldBe(2);
        entities.Select(e => e.Id).ShouldBe([1, 2]);
        entities.ShouldAllBe(e => e.EstateId == 0);
    }
}
