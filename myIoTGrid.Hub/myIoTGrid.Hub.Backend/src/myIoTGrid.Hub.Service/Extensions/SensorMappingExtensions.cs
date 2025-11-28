using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Sensor Entity (Physical sensor chip: DHT22, BME280).
/// Matter-konform: Entspricht einem Matter Endpoint.
/// </summary>
public static class SensorMappingExtensions
{
    /// <summary>
    /// Converts a Sensor entity to a SensorDto
    /// </summary>
    public static SensorDto ToDto(this Sensor sensor)
    {
        return new SensorDto(
            Id: sensor.Id,
            NodeId: sensor.NodeId,
            SensorTypeId: sensor.SensorTypeId,
            EndpointId: sensor.EndpointId,
            Name: sensor.Name,
            IsActive: sensor.IsActive,
            SensorType: sensor.SensorType?.ToDto(),
            CreatedAt: sensor.CreatedAt
        );
    }

    /// <summary>
    /// Converts a CreateSensorDto to a Sensor entity
    /// </summary>
    public static Sensor ToEntity(this CreateSensorDto dto, Guid nodeId)
    {
        return new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = nodeId,
            SensorTypeId = dto.SensorTypeId,
            EndpointId = dto.EndpointId,
            Name = dto.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies an UpdateSensorDto to a Sensor entity
    /// </summary>
    public static void ApplyUpdate(this Sensor sensor, UpdateSensorDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Name))
            sensor.Name = dto.Name;

        if (dto.IsActive.HasValue)
            sensor.IsActive = dto.IsActive.Value;
    }

    /// <summary>
    /// Converts a list of Sensor Entities to DTOs
    /// </summary>
    public static IEnumerable<SensorDto> ToDtos(this IEnumerable<Sensor> sensors)
    {
        return sensors.Select(s => s.ToDto());
    }
}
