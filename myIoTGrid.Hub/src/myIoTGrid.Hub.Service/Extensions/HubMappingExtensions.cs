
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Hub (Raspberry Pi Gateway)
/// </summary>
public static class HubMappingExtensions
{
    /// <summary>
    /// Converts Hub Entity to HubDto
    /// Single-Hub-Architecture: IsOnline is always true when API is reachable
    /// </summary>
    public static HubDto ToDto(this myIoTGrid.Shared.Common.Entities.Hub entity)
    {
        return new HubDto(
            entity.Id,
            entity.TenantId,
            entity.HubId,
            entity.Name,
            entity.Description,
            DateTime.UtcNow, // LastSeen is now (API is responding)
            true, // Always online when API is reachable
            entity.CreatedAt,
            entity.Nodes.Count,
            entity.DefaultWifiSsid,
            entity.DefaultWifiPassword,
            entity.ApiUrl,
            entity.ApiPort
        );
    }

    /// <summary>
    /// Converts Hub Entity to HubDto (without Nodes collection loaded)
    /// Single-Hub-Architecture: IsOnline is always true when API is reachable
    /// </summary>
    public static HubDto ToDto(this myIoTGrid.Shared.Common.Entities.Hub entity, int nodeCount)
    {
        return new HubDto(
            entity.Id,
            entity.TenantId,
            entity.HubId,
            entity.Name,
            entity.Description,
            DateTime.UtcNow, // LastSeen is now (API is responding)
            true, // Always online when API is reachable
            entity.CreatedAt,
            nodeCount,
            entity.DefaultWifiSsid,
            entity.DefaultWifiPassword,
            entity.ApiUrl,
            entity.ApiPort
        );
    }

    /// <summary>
    /// Converts Hub Entity to HubProvisioningSettingsDto
    /// </summary>
    public static HubProvisioningSettingsDto ToProvisioningSettingsDto(this myIoTGrid.Shared.Common.Entities.Hub entity)
    {
        return new HubProvisioningSettingsDto(
            entity.DefaultWifiSsid,
            entity.DefaultWifiPassword,
            entity.ApiUrl,
            entity.ApiPort
        );
    }

    /// <summary>
    /// Converts CreateHubDto to Hub Entity
    /// </summary>
    public static myIoTGrid.Shared.Common.Entities.Hub ToEntity(this CreateHubDto dto, Guid tenantId)
    {
        return new myIoTGrid.Shared.Common.Entities.Hub
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
    public static void ApplyUpdate(this myIoTGrid.Shared.Common.Entities.Hub entity, UpdateHubDto dto)
    {
        if (dto.Name != null)
            entity.Name = dto.Name;
        if (dto.Description != null)
            entity.Description = dto.Description;
        if (dto.DefaultWifiSsid != null)
            entity.DefaultWifiSsid = dto.DefaultWifiSsid;
        if (dto.DefaultWifiPassword != null)
            entity.DefaultWifiPassword = dto.DefaultWifiPassword;
        if (dto.ApiUrl != null)
            entity.ApiUrl = dto.ApiUrl;
        if (dto.ApiPort.HasValue)
            entity.ApiPort = dto.ApiPort.Value;
    }

    /// <summary>
    /// Converts a list of Hub Entities to DTOs
    /// </summary>
    public static IEnumerable<HubDto> ToDtos(this IEnumerable<myIoTGrid.Shared.Common.Entities.Hub> entities)
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
