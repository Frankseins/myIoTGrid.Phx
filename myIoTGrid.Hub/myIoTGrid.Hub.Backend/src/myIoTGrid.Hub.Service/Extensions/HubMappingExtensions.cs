using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Hub (Raspberry Pi Gateway)
/// </summary>
public static class HubMappingExtensions
{
    /// <summary>
    /// Converts Hub Entity to HubDto
    /// </summary>
    public static HubDto ToDto(this Domain.Entities.Hub entity)
    {
        return new HubDto(
            entity.Id,
            entity.TenantId,
            entity.HubId,
            entity.Name,
            entity.Description,
            entity.LastSeen,
            entity.IsOnline,
            entity.CreatedAt,
            entity.Sensors.Count
        );
    }

    /// <summary>
    /// Converts Hub Entity to HubDto (without Sensors collection loaded)
    /// </summary>
    public static HubDto ToDto(this Domain.Entities.Hub entity, int sensorCount)
    {
        return new HubDto(
            entity.Id,
            entity.TenantId,
            entity.HubId,
            entity.Name,
            entity.Description,
            entity.LastSeen,
            entity.IsOnline,
            entity.CreatedAt,
            sensorCount
        );
    }

    /// <summary>
    /// Converts CreateHubDto to Hub Entity
    /// </summary>
    public static Domain.Entities.Hub ToEntity(this CreateHubDto dto, Guid tenantId)
    {
        return new Domain.Entities.Hub
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HubId = dto.HubId,
            Name = dto.Name ?? GenerateNameFromHubId(dto.HubId),
            Description = dto.Description,
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies UpdateHubDto to Hub Entity
    /// </summary>
    public static void ApplyUpdate(this Domain.Entities.Hub entity, UpdateHubDto dto)
    {
        if (dto.Name != null)
            entity.Name = dto.Name;
        if (dto.Description != null)
            entity.Description = dto.Description;
    }

    /// <summary>
    /// Converts a list of Hub Entities to DTOs
    /// </summary>
    public static IEnumerable<HubDto> ToDtos(this IEnumerable<Domain.Entities.Hub> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    /// <summary>
    /// Generates a name from the HubId
    /// </summary>
    private static string GenerateNameFromHubId(string hubId)
    {
        if (string.IsNullOrWhiteSpace(hubId)) return "Unknown Hub";

        // "hub-home-01" -> "Hub Home 01"
        var parts = hubId.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var formattedParts = parts.Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : s);

        return string.Join(" ", formattedParts);
    }
}
