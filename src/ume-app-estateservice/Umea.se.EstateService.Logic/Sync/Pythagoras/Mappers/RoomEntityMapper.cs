using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers;

/// <summary>
/// Maps Workspace DTOs from Pythagoras API to RoomEntity objects.
/// </summary>
public static class RoomEntityMapper
{
    /// <summary>
    /// Converts a Workspace DTO to a RoomEntity.
    /// </summary>
    /// <param name="dto">The Workspace DTO from Pythagoras API.</param>
    /// <returns>A mapped RoomEntity.</returns>
    public static RoomEntity ToEntity(Workspace dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RoomEntity
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName ?? string.Empty,
            GrossArea = dto.GrossArea,
            NetArea = dto.NetArea,
            Capacity = dto.Capacity,
            BuildingId = dto.BuildingId ?? 0,
            FloorId = dto.FloorId,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(dto.Updated / 1000)
        };
    }

    /// <summary>
    /// Converts a collection of Workspace DTOs to RoomEntity objects, skipping Pythagoras
    /// placeholder workspaces that do not represent real rooms.
    /// </summary>
    public static List<RoomEntity> ToEntities(IReadOnlyList<Workspace> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return MapperUtilities.ToEntities([.. dtos.Where(dto => !IsPlaceholder(dto))], ToEntity);
    }

    /// <summary>
    /// Pythagoras uses "R01" as a stand-in room for buildings without room information, and
    /// SpaceManager leaves behind workspaces named "unset". Neither is a real room.
    /// </summary>
    internal static bool IsPlaceholder(Workspace dto)
        => string.Equals(dto.Name, "R01", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(dto.Name, "unset", StringComparison.OrdinalIgnoreCase);
}
