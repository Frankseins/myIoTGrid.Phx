using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Reading Entity (Measurement).
/// Matter-konform: Entspricht einem Attribute Report.
/// </summary>
public static class ReadingMappingExtensions
{
    /// <summary>
    /// Converts a Reading entity to a ReadingDto
    /// </summary>
    public static ReadingDto ToDto(this Reading reading, LocationDto? location = null)
    {
        return new ReadingDto(
            Id: reading.Id,
            TenantId: reading.TenantId,
            NodeId: reading.NodeId,
            SensorTypeId: reading.SensorTypeId,
            SensorTypeName: reading.SensorType?.DisplayName ?? reading.SensorTypeId,
            Value: reading.Value,
            Unit: reading.SensorType?.Unit ?? string.Empty,
            Timestamp: reading.Timestamp,
            Location: location ?? reading.Node?.Location?.ToDto(),
            IsSyncedToCloud: reading.IsSyncedToCloud
        );
    }

    /// <summary>
    /// Converts a CreateReadingDto to a Reading entity
    /// </summary>
    public static Reading ToEntity(this CreateReadingDto dto, Guid tenantId, Guid nodeId)
    {
        return new Reading
        {
            TenantId = tenantId,
            NodeId = nodeId,
            SensorTypeId = dto.Type.ToLowerInvariant(),
            Value = dto.Value,
            Timestamp = dto.Timestamp ?? DateTime.UtcNow,
            IsSyncedToCloud = false
        };
    }

    /// <summary>
    /// Converts a list of Reading Entities to DTOs
    /// </summary>
    public static IEnumerable<ReadingDto> ToDtos(this IEnumerable<Reading> readings)
    {
        return readings.Select(r => r.ToDto());
    }
}
