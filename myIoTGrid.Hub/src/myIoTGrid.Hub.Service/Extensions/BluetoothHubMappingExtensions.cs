namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for BluetoothHub (Bluetooth Gateway)
/// </summary>
public static class BluetoothHubMappingExtensions
{
    /// <summary>
    /// Converts BluetoothHub Entity to BluetoothHubDto
    /// </summary>
    public static BluetoothHubDto ToDto(this BluetoothHub entity)
    {
        return new BluetoothHubDto(
            entity.Id,
            entity.HubId,
            entity.Name,
            entity.MacAddress,
            entity.Status,
            entity.LastSeen,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Nodes.Count
        );
    }

    /// <summary>
    /// Converts BluetoothHub Entity to BluetoothHubDto (without Nodes collection loaded)
    /// </summary>
    public static BluetoothHubDto ToDto(this BluetoothHub entity, int nodeCount)
    {
        return new BluetoothHubDto(
            entity.Id,
            entity.HubId,
            entity.Name,
            entity.MacAddress,
            entity.Status,
            entity.LastSeen,
            entity.CreatedAt,
            entity.UpdatedAt,
            nodeCount
        );
    }

    /// <summary>
    /// Converts CreateBluetoothHubDto to BluetoothHub Entity
    /// </summary>
    public static BluetoothHub ToEntity(this CreateBluetoothHubDto dto, Guid hubId)
    {
        return new BluetoothHub
        {
            Id = Guid.NewGuid(),
            HubId = dto.HubId ?? hubId,
            Name = dto.Name,
            MacAddress = dto.MacAddress,
            Status = "Inactive",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies UpdateBluetoothHubDto to BluetoothHub Entity
    /// </summary>
    public static void ApplyUpdate(this BluetoothHub entity, UpdateBluetoothHubDto dto)
    {
        if (dto.Name != null)
            entity.Name = dto.Name;
        if (dto.MacAddress != null)
            entity.MacAddress = dto.MacAddress;
        if (dto.Status != null)
            entity.Status = dto.Status;

        entity.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Converts a list of BluetoothHub Entities to DTOs
    /// </summary>
    public static IEnumerable<BluetoothHubDto> ToDtos(this IEnumerable<BluetoothHub> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}
