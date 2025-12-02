using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for Node (ESP32/LoRa32 device) information.
/// Matter-konform: Entspricht einem Matter Node.
/// </summary>
public record NodeDto(
    Guid Id,
    Guid HubId,
    string NodeId,
    string Name,
    ProtocolDto Protocol,
    LocationDto? Location,
    int AssignmentCount,
    DateTime? LastSeen,
    bool IsOnline,
    string? FirmwareVersion,
    int? BatteryLevel,
    DateTime CreatedAt
);

/// <summary>
/// DTO for creating a Node
/// </summary>
public record CreateNodeDto(
    string NodeId,
    string? Name = null,
    string? HubIdentifier = null,
    Guid? HubId = null,
    ProtocolDto Protocol = ProtocolDto.WLAN,
    LocationDto? Location = null
);

/// <summary>
/// DTO for updating a Node
/// </summary>
public record UpdateNodeDto(
    string? Name = null,
    LocationDto? Location = null,
    string? FirmwareVersion = null
);

/// <summary>
/// DTO for Node status updates
/// </summary>
public record NodeStatusDto(
    Guid NodeId,
    bool IsOnline,
    DateTime? LastSeen,
    int? BatteryLevel
);

/// <summary>
/// DTO for sensor/device registration (from ESP32/LoRa32 devices)
/// </summary>
public record RegisterNodeDto(
    string SerialNumber,
    string? FirmwareVersion = null,
    string? HardwareType = null,
    List<string>? Capabilities = null,
    string? Name = null,
    LocationDto? Location = null
);

/// <summary>
/// Response DTO for node registration
/// Contains configuration for the sensor device to initialize its sensors
/// </summary>
public record NodeRegistrationResponseDto(
    Guid NodeId,
    string SerialNumber,
    string Name,
    string? Location,
    int IntervalSeconds,
    List<SensorConfigDto> Sensors,
    ConnectionConfigDto Connection,
    bool IsNewNode,
    string Message
);

/// <summary>
/// Sensor configuration for device registration response
/// </summary>
public record SensorConfigDto(
    string Type,
    bool Enabled,
    int Pin = -1
);

/// <summary>
/// Connection configuration for device registration response
/// </summary>
public record ConnectionConfigDto(
    string Mode,
    string Endpoint
);
