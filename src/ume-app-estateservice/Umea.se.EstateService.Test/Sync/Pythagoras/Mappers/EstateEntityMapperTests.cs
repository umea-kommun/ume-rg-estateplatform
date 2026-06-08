using Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Test.Sync.Pythagoras.Mappers;

public class EstateEntityMapperTests
{
    private static Dictionary<int, PropertyValueDto> Props(params (PropertyCategoryId Id, string? Value)[] values)
        => values.ToDictionary(v => (int)v.Id, v => new PropertyValueDto { Value = v.Value });

    [Fact]
    public void ToEntity_NullDto_Throws()
    {
        Should.Throw<ArgumentNullException>(() => EstateEntityMapper.ToEntity(null!));
    }

    [Fact]
    public void ToEntity_MapsCoreScalarFields()
    {
        Guid uid = Guid.NewGuid();
        NavigationFolder dto = new()
        {
            Id = 11,
            Uid = uid,
            Name = "Räven 1",
            PopularName = "Räven",
            Grossarea = 5000m,
            Netarea = 4200m
        };

        EstateEntity entity = EstateEntityMapper.ToEntity(dto);

        entity.Id.ShouldBe(11);
        entity.Uid.ShouldBe(uid);
        entity.Name.ShouldBe("Räven 1");
        entity.PopularName.ShouldBe("Räven");
        entity.GrossArea.ShouldBe(5000m);
        entity.NetArea.ShouldBe(4200m);
        entity.Buildings.ShouldBeEmpty();
    }

    [Fact]
    public void ToEntity_NullAreas_DefaultToZero()
    {
        EstateEntity entity = EstateEntityMapper.ToEntity(new NavigationFolder { Id = 1, Grossarea = null, Netarea = null });

        entity.GrossArea.ShouldBe(0m);
        entity.NetArea.ShouldBe(0m);
    }

    [Fact]
    public void ToEntity_SetsUpdatedAtToRecentUtc()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-5);

        EstateEntity entity = EstateEntityMapper.ToEntity(new NavigationFolder { Id = 1 });

        entity.UpdatedAt.ShouldBeInRange(before, DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void ToEntity_BothCoordinatesZero_GeoLocationNull()
    {
        EstateEntity entity = EstateEntityMapper.ToEntity(new NavigationFolder { Id = 1, GeoX = 0, GeoY = 0 });

        entity.GeoLocation.ShouldBeNull();
    }

    [Theory]
    [InlineData(63.8258, 20.2630)]
    [InlineData(0, 20.2630)]
    [InlineData(63.8258, 0)]
    public void ToEntity_AnyNonZeroCoordinate_MapsGeoLocation(double geoX, double geoY)
    {
        EstateEntity entity = EstateEntityMapper.ToEntity(new NavigationFolder { Id = 1, GeoX = geoX, GeoY = geoY });

        entity.GeoLocation.ShouldNotBeNull();
        entity.GeoLocation!.Lat.ShouldBe(geoX);
        entity.GeoLocation.Lon.ShouldBe(geoY);
    }

    [Fact]
    public void ToEntity_NoAddressFields_AddressNull()
    {
        EstateEntity entity = EstateEntityMapper.ToEntity(new NavigationFolder { Id = 1 });

        entity.Address.ShouldBeNull();
    }

    [Theory]
    [InlineData("Skolgatan 31A", null, null, null, null)]
    [InlineData(null, "901 84", null, null, null)]
    [InlineData(null, null, "Umeå", null, null)]
    [InlineData(null, null, null, "Sverige", null)]
    [InlineData(null, null, null, null, "Plan 2")]
    public void ToEntity_AnyAddressFieldPresent_MapsAddress(string? street, string? zip, string? city, string? country, string? extra)
    {
        NavigationFolder dto = new()
        {
            Id = 1,
            AddressStreet = street,
            AddressZipCode = zip,
            AddressCity = city,
            AddressCountry = country,
            AddressExtra = extra
        };

        EstateEntity entity = EstateEntityMapper.ToEntity(dto);

        entity.Address.ShouldNotBeNull();
        entity.Address!.Street.ShouldBe(street ?? string.Empty);
        entity.Address.ZipCode.ShouldBe(zip ?? string.Empty);
        entity.Address.City.ShouldBe(city ?? string.Empty);
        entity.Address.Country.ShouldBe(country ?? string.Empty);
        entity.Address.Extra.ShouldBe(extra ?? string.Empty);
    }

    [Fact]
    public void ToEntity_MapsPropertyValueBackedScalars()
    {
        NavigationFolder dto = new()
        {
            Id = 1,
            PropertyValues = Props(
                (PropertyCategoryId.PropertyDesignation, "Kvarteret Räven 1"),
                (PropertyCategoryId.OperationalArea, "Centrum"),
                (PropertyCategoryId.AdministrativeArea, "Väster"),
                (PropertyCategoryId.MunicipalityArea, "Umeå"),
                (PropertyCategoryId.EstateExternalStatus, "Egen"),
                (PropertyCategoryId.EstateExternalOwnerName, "Acme AB"),
                (PropertyCategoryId.EstateExternalOwnerNote, "Note"))
        };

        EstateEntity entity = EstateEntityMapper.ToEntity(dto);

        entity.PropertyDesignation.ShouldBe("Kvarteret Räven 1");
        entity.OperationalArea.ShouldBe("Centrum");
        entity.AdministrativeArea.ShouldBe("Väster");
        entity.MunicipalityArea.ShouldBe("Umeå");
        entity.ExternalOwnerStatus.ShouldBe("Egen");
        entity.ExternalOwnerName.ShouldBe("Acme AB");
        entity.ExternalOwnerNote.ShouldBe("Note");
    }

    [Fact]
    public void ToEntity_NullPropertyValues_LeavesScalarsNull()
    {
        EstateEntity entity = EstateEntityMapper.ToEntity(new NavigationFolder { Id = 1 });

        entity.PropertyDesignation.ShouldBeNull();
        entity.OperationalArea.ShouldBeNull();
        entity.ExternalOwnerStatus.ShouldBeNull();
    }

    [Fact]
    public void ToEntity_NullBuildings_BuildingCountZero()
    {
        EstateEntity entity = EstateEntityMapper.ToEntity(new NavigationFolder { Id = 1, Buildings = null });

        entity.BuildingCount.ShouldBe(0);
    }

    [Fact]
    public void ToEntity_WithBuildings_BuildingCountMatchesLength()
    {
        EstateEntity entity = EstateEntityMapper.ToEntity(new NavigationFolder
        {
            Id = 1,
            Buildings = [new Building { Id = 1 }, new Building { Id = 2 }, new Building { Id = 3 }]
        });

        entity.BuildingCount.ShouldBe(3);
    }

    [Fact]
    public void ToEntities_EmptyList_ReturnsEmpty()
    {
        EstateEntityMapper.ToEntities([]).ShouldBeEmpty();
    }

    [Fact]
    public void ToEntities_MapsEachDto()
    {
        List<EstateEntity> entities = EstateEntityMapper.ToEntities(
        [
            new NavigationFolder { Id = 1, Name = "A" },
            new NavigationFolder { Id = 2, Name = "B" }
        ]);

        entities.Count.ShouldBe(2);
        entities.Select(e => e.Id).ShouldBe([1, 2]);
    }
}
