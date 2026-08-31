using System.Text.Json.Serialization;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingInfoModel : ISearchable, IFavoriteable
{
    [JsonIgnore]
    public NodeType FavoriteNodeType => NodeType.Building;
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public GeoPointModel? GeoLocation { get; init; }
    public decimal GrossArea { get; init; }
    public decimal NetArea { get; init; }
    public decimal SumGrossFloorArea { get; init; }
    public int NumPlacedPersons { get; init; }
    public int? NumFloors { get; set; }
    public int? NumRooms { get; set; }
    public int? NumDocuments { get; set; }
    public BusinessTypeModel? BusinessType { get; set; }
    public AddressModel? Address { get; init; }
    public BuildingAscendantModel? Estate { get; set; }
    public BuildingAscendantModel? Region { get; set; }
    public BuildingAscendantModel? Organization { get; set; }
    public BuildingExtendedPropertiesModel? ExtendedProperties { get; init; }
    /// <summary>
    /// What the building itself offers, before the per-user group gate. Server-side plumbing for
    /// <see cref="WorkOrderTypeAccess"/>, which is what clients read.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<WorkOrderType> WorkOrderTypes { get; set; } = [];

    /// <summary>
    /// Per-type access for the current user on this building. A type gated behind an AD group the
    /// user lacks is absent altogether, so the client hides it; a present type is either enabled or
    /// disabled depending on whether this building offers it. The client renders this as given and
    /// derives no access rules of its own.
    /// </summary>
    public IReadOnlyDictionary<WorkOrderType, WorkOrderAccessState> WorkOrderTypeAccess { get; set; }
        = new Dictionary<WorkOrderType, WorkOrderAccessState>();

    public string? ImageUrl { get; set; }
    public bool? IsFavorite { get; set; }
    public DateTimeOffset UpdatedAt => DateTime.Now;
}
