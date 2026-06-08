using System.ComponentModel.DataAnnotations;
using Umea.se.EstateService.API.Requests;
using Umea.se.EstateService.Shared.Autocomplete;

namespace Umea.se.EstateService.Test.API.Controllers.Requests;

public class SearchRequestTests
{
    [Fact]
    public void Validate_AllowsGeoOnlySearch()
    {
        SearchRequest request = new()
        {
            Query = string.Empty,
            Latitude = 63.8258,
            Longitude = 20.2630,
            RadiusMeters = 1_000
        };

        IList<ValidationResult> results = Validate(request);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ReturnsError_ForInvalidLatitude()
    {
        SearchRequest request = new()
        {
            Query = string.Empty,
            Latitude = 120,
            Longitude = 10,
            RadiusMeters = 1_000
        };

        IList<ValidationResult> results = Validate(request);

        results.ShouldContain(result => result.MemberNames.Contains(nameof(SearchRequest.Latitude)));
    }

    [Fact]
    public void Validate_ReturnsError_WhenGeoParametersIncomplete()
    {
        SearchRequest request = new()
        {
            Query = string.Empty,
            Latitude = 63.8258
        };

        IList<ValidationResult> results = Validate(request);

        results.ShouldContain(result => result.MemberNames.Contains(nameof(SearchRequest.RadiusMeters)));
    }

    [Fact]
    public void Validate_EmptyRequest_HasNoErrors()
    {
        Validate(new SearchRequest()).ShouldBeEmpty();
    }

    [Fact]
    public void Validate_AnyCombinedWithOtherType_ReturnsError()
    {
        SearchRequest request = new()
        {
            Type = [AutocompleteType.Any, AutocompleteType.Building]
        };

        Validate(request).ShouldContain(r => r.MemberNames.Contains(nameof(SearchRequest.Type)));
    }

    [Fact]
    public void Validate_AnyTypeAlone_NoTypeError()
    {
        SearchRequest request = new()
        {
            Type = [AutocompleteType.Any]
        };

        Validate(request).ShouldNotContain(r => r.MemberNames.Contains(nameof(SearchRequest.Type)));
    }

    [Fact]
    public void Validate_MultipleConcreteTypes_NoTypeError()
    {
        SearchRequest request = new()
        {
            Type = [AutocompleteType.Building, AutocompleteType.Estate]
        };

        Validate(request).ShouldNotContain(r => r.MemberNames.Contains(nameof(SearchRequest.Type)));
    }

    [Fact]
    public void Validate_RadiusAndBoundingBoxTogether_ReturnsError()
    {
        SearchRequest request = new()
        {
            Latitude = 63.8,
            Longitude = 20.2,
            RadiusMeters = 1_000,
            SouthLatitude = 63,
            WestLongitude = 20,
            NorthLatitude = 64,
            EastLongitude = 21
        };

        Validate(request).ShouldContain(r => r.ErrorMessage!.Contains("either a radius-based geo filter or a bounding box"));
    }

    [Theory]
    [InlineData(63.8, null, 1_000)]
    [InlineData(63.8, 20.2, null)]
    [InlineData(null, 20.2, 1_000)]
    public void Validate_RadiusGeoIncomplete_ReturnsTogetherError(double? lat, double? lon, int? radius)
    {
        SearchRequest request = new()
        {
            Latitude = lat,
            Longitude = lon,
            RadiusMeters = radius
        };

        Validate(request).ShouldContain(r => r.ErrorMessage!.Contains("Latitude, longitude, and radius must be provided together"));
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_RadiusGeoLatitudeOutOfRange_ReturnsError(double latitude)
    {
        SearchRequest request = new()
        {
            Latitude = latitude,
            Longitude = 20,
            RadiusMeters = 1_000
        };

        Validate(request).ShouldContain(r => r.MemberNames.Contains(nameof(SearchRequest.Latitude)));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Validate_RadiusGeoLongitudeOutOfRange_ReturnsError(double longitude)
    {
        SearchRequest request = new()
        {
            Latitude = 63.8,
            Longitude = longitude,
            RadiusMeters = 1_000
        };

        Validate(request).ShouldContain(r => r.MemberNames.Contains(nameof(SearchRequest.Longitude)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(SearchRequest.MaxRadiusMeters + 1)]
    public void Validate_RadiusOutOfRange_ReturnsError(int radius)
    {
        SearchRequest request = new()
        {
            Latitude = 63.8,
            Longitude = 20.2,
            RadiusMeters = radius
        };

        Validate(request).ShouldContain(r => r.MemberNames.Contains(nameof(SearchRequest.RadiusMeters)));
    }

    [Fact]
    public void Validate_RadiusGeoNaNCoordinates_ReturnsErrors()
    {
        SearchRequest request = new()
        {
            Latitude = double.NaN,
            Longitude = double.NaN,
            RadiusMeters = 1_000
        };

        IList<ValidationResult> results = Validate(request);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(SearchRequest.Latitude)));
        results.ShouldContain(r => r.MemberNames.Contains(nameof(SearchRequest.Longitude)));
    }

    [Fact]
    public void Validate_ValidBoundingBox_NoErrors()
    {
        SearchRequest request = new()
        {
            SouthLatitude = 63.80,
            WestLongitude = 20.20,
            NorthLatitude = 63.85,
            EastLongitude = 20.30
        };

        Validate(request).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, 20.2, 63.85, 20.3)]
    [InlineData(63.8, null, 63.85, 20.3)]
    [InlineData(63.8, 20.2, null, 20.3)]
    [InlineData(63.8, 20.2, 63.85, null)]
    public void Validate_BoundingBoxIncomplete_ReturnsTogetherError(double? south, double? west, double? north, double? east)
    {
        SearchRequest request = new()
        {
            SouthLatitude = south,
            WestLongitude = west,
            NorthLatitude = north,
            EastLongitude = east
        };

        Validate(request).ShouldContain(r => r.ErrorMessage!.Contains("South, west, north, and east must be provided together"));
    }

    [Fact]
    public void Validate_BoundingBoxSouthNotLessThanNorth_ReturnsError()
    {
        SearchRequest request = new()
        {
            SouthLatitude = 64.0,
            WestLongitude = 20.2,
            NorthLatitude = 63.8,
            EastLongitude = 20.3
        };

        Validate(request).ShouldContain(r => r.ErrorMessage!.Contains("South latitude must be less than north latitude"));
    }

    [Fact]
    public void Validate_BoundingBoxWestNotLessThanEast_ReturnsError()
    {
        SearchRequest request = new()
        {
            SouthLatitude = 63.8,
            WestLongitude = 20.4,
            NorthLatitude = 63.9,
            EastLongitude = 20.3
        };

        Validate(request).ShouldContain(r => r.ErrorMessage!.Contains("West longitude must be less than east longitude"));
    }

    [Theory]
    [InlineData(-91, 20.2, 90, 20.3, nameof(SearchRequest.SouthLatitude))]
    [InlineData(63.8, 20.2, 91, 20.3, nameof(SearchRequest.NorthLatitude))]
    [InlineData(63.8, -181, 63.9, 20.3, nameof(SearchRequest.WestLongitude))]
    [InlineData(63.8, 20.2, 63.9, 181, nameof(SearchRequest.EastLongitude))]
    public void Validate_BoundingBoxCoordinateOutOfRange_ReturnsError(double south, double west, double north, double east, string member)
    {
        SearchRequest request = new()
        {
            SouthLatitude = south,
            WestLongitude = west,
            NorthLatitude = north,
            EastLongitude = east
        };

        Validate(request).ShouldContain(r => r.MemberNames.Contains(member));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SearchRequest.MaxLimit + 1)]
    public void Validate_LimitOutOfRange_ReturnsError(int limit)
    {
        SearchRequest request = new() { Limit = limit };

        Validate(request).ShouldContain(r => r.MemberNames.Contains(nameof(SearchRequest.Limit)));
    }

    [Fact]
    public void Validate_LimitWithinRange_NoError()
    {
        SearchRequest request = new() { Limit = 500 };

        Validate(request).ShouldNotContain(r => r.MemberNames.Contains(nameof(SearchRequest.Limit)));
    }

    private static IList<ValidationResult> Validate(SearchRequest request)
    {
        ValidationContext context = new(request);
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, context, results, validateAllProperties: true);
        return results;
    }
}
