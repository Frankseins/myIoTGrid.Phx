using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for Sensor (ESP32/LoRa32 device) information
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="HubId">Hub ID (FK)</param>
/// <param name="SensorId">Sensor identifier (e.g., "sensor-wohnzimmer-01")</param>
/// <param name="Name">Display name</param>
/// <param name="Protocol">Communication protocol</param>
/// <param name="Location">Physical location of the sensor</param>
/// <param name="SensorTypes">List of sensor types this device can measure</param>
/// <param name="LastSeen">Last data received</param>
/// <param name="IsOnline">Online status</param>
/// <param name="FirmwareVersion">Firmware version (if reported)</param>
/// <param name="BatteryLevel">Battery level in percent (for battery-powered sensors)</param>
/// <param name="CreatedAt">Creation timestamp</param>
public record SensorDto(
    Guid Id,
    Guid HubId,
    string SensorId,
    string Name,
    ProtocolDto Protocol,
    LocationDto? Location,
    List<string> SensorTypes,
    DateTime? LastSeen,
    bool IsOnline,
    string? FirmwareVersion,
    int? BatteryLevel,
    DateTime CreatedAt
);

/// <summary>
/// DTO for creating a Sensor
/// </summary>
/// <param name="HubId">Hub ID (FK) - if using internal Guid</param>
/// <param name="HubIdentifier">Hub identifier string (e.g., "hub-home-01") - for auto-registration</param>
/// <param name="SensorId">Sensor identifier (e.g., "sensor-wohnzimmer-01")</param>
/// <param name="Name">Display name (optional, will be generated from SensorId)</param>
/// <param name="Protocol">Communication protocol</param>
/// <param name="Location">Physical location</param>
/// <param name="SensorTypes">List of sensor types</param>
public record CreateSensorDto(
    string SensorId,
    string? Name = null,
    string? HubIdentifier = null,
    Guid? HubId = null,
    ProtocolDto Protocol = ProtocolDto.WLAN,
    LocationDto? Location = null,
    List<string>? SensorTypes = null
);

/// <summary>
/// DTO for updating a Sensor
/// </summary>
/// <param name="Name">New display name</param>
/// <param name="Location">New location</param>
/// <param name="SensorTypes">New sensor types list</param>
/// <param name="FirmwareVersion">New firmware version</param>
public record UpdateSensorDto(
    string? Name = null,
    LocationDto? Location = null,
    List<string>? SensorTypes = null,
    string? FirmwareVersion = null
);

/// <summary>
/// DTO for Sensor status updates
/// </summary>
/// <param name="SensorId">Sensor ID</param>
/// <param name="IsOnline">Online status</param>
/// <param name="LastSeen">Last seen timestamp</param>
/// <param name="BatteryLevel">Battery level</param>
public record SensorStatusDto(
    Guid SensorId,
    bool IsOnline,
    DateTime? LastSeen,
    int? BatteryLevel
);
