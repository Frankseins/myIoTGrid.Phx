using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Sensor (ESP32/LoRa32 Device)
/// </summary>
public static class SensorMappingExtensions
{
    /// <summary>
    /// Converts Sensor Entity to SensorDto
    /// </summary>
    public static SensorDto ToDto(this Sensor entity)
    {
        return new SensorDto(
            entity.Id,
            entity.HubId,
            entity.SensorId,
            entity.Name,
            entity.Protocol.ToDto(),
            entity.Location.ToDto(),
            entity.SensorTypes,
            entity.LastSeen,
            entity.IsOnline,
            entity.FirmwareVersion,
            entity.BatteryLevel,
            entity.CreatedAt
        );
    }

    /// <summary>
    /// Converts CreateSensorDto to Sensor Entity
    /// </summary>
    public static Sensor ToEntity(this CreateSensorDto dto, Guid hubId)
    {
        return new Sensor
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            SensorId = dto.SensorId,
            Name = dto.Name ?? GenerateNameFromSensorId(dto.SensorId),
            Protocol = dto.Protocol.ToEntity(),
            Location = dto.Location.ToEntity(),
            SensorTypes = dto.SensorTypes ?? new List<string>(),
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Applies UpdateSensorDto to Sensor Entity
    /// </summary>
    public static void ApplyUpdate(this Sensor entity, UpdateSensorDto dto)
    {
        if (dto.Name != null)
            entity.Name = dto.Name;
        if (dto.Location != null)
            entity.Location = dto.Location.ToEntity();
        if (dto.SensorTypes != null)
            entity.SensorTypes = dto.SensorTypes;
        if (dto.FirmwareVersion != null)
            entity.FirmwareVersion = dto.FirmwareVersion;
    }

    /// <summary>
    /// Applies SensorStatusDto to Sensor Entity
    /// </summary>
    public static void ApplyStatus(this Sensor entity, SensorStatusDto status)
    {
        entity.IsOnline = status.IsOnline;
        if (status.LastSeen.HasValue)
            entity.LastSeen = status.LastSeen.Value;
        if (status.BatteryLevel.HasValue)
            entity.BatteryLevel = status.BatteryLevel.Value;
    }

    /// <summary>
    /// Converts a list of Sensor Entities to DTOs
    /// </summary>
    public static IEnumerable<SensorDto> ToDtos(this IEnumerable<Sensor> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    /// <summary>
    /// Generates a name from the SensorId
    /// </summary>
    private static string GenerateNameFromSensorId(string sensorId)
    {
        if (string.IsNullOrWhiteSpace(sensorId)) return "Unknown Sensor";

        // "sensor-wohnzimmer-01" -> "Sensor Wohnzimmer 01"
        var parts = sensorId.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var formattedParts = parts.Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : s);

        return string.Join(" ", formattedParts);
    }
}
