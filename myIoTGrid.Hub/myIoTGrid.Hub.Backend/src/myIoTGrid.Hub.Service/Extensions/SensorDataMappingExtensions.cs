using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for SensorData
/// </summary>
public static class SensorDataMappingExtensions
{
    /// <summary>
    /// Converts SensorData Entity to SensorDataDto
    /// </summary>
    public static SensorDataDto ToDto(this SensorData entity, SensorType sensorType, LocationDto? location = null)
    {
        return new SensorDataDto(
            entity.Id,
            entity.TenantId,
            entity.SensorId,
            entity.SensorTypeId,
            sensorType.Code,
            sensorType.Name,
            sensorType.Unit,
            entity.Value,
            entity.Timestamp,
            location,
            entity.IsSyncedToCloud
        );
    }

    /// <summary>
    /// Converts SensorData Entity to SensorDataDto (when Sensor and SensorType are loaded)
    /// </summary>
    public static SensorDataDto ToDto(this SensorData entity)
    {
        if (entity.SensorType == null)
            throw new InvalidOperationException("SensorType must be loaded to convert to DTO");

        var location = entity.Sensor?.Location.ToDto();

        return entity.ToDto(entity.SensorType, location);
    }

    /// <summary>
    /// Converts CreateSensorDataDto to SensorData Entity
    /// </summary>
    public static SensorData ToEntity(this CreateSensorDataDto dto, Guid tenantId, Guid sensorId, Guid sensorTypeId)
    {
        return new SensorData
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SensorId = sensorId,
            SensorTypeId = sensorTypeId,
            Value = dto.Value,
            Timestamp = dto.Timestamp ?? DateTime.UtcNow,
            IsSyncedToCloud = false
        };
    }

    /// <summary>
    /// Converts a list of SensorData Entities to DTOs
    /// </summary>
    public static IEnumerable<SensorDataDto> ToDtos(this IEnumerable<SensorData> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}
