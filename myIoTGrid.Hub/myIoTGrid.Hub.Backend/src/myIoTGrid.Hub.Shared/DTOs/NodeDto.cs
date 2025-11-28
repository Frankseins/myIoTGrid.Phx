using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for Node (ESP32/LoRa32 device) information.
/// Matter-konform: Entspricht einem Matter Node.
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="HubId">Hub ID (FK)</param>
/// <param name="NodeId">Node identifier (e.g., "wetterstation-garten-01")</param>
/// <param name="Name">Display name</param>
/// <param name="Protocol">Communication protocol</param>
/// <param name="Location">Physical location of the node</param>
/// <param name="Sensors">List of sensors attached to this node</param>
/// <param name="LastSeen">Last contact timestamp</param>
/// <param name="IsOnline">Online status</param>
/// <param name="FirmwareVersion">Firmware version (if reported)</param>
/// <param name="BatteryLevel">Battery level in percent (for battery-powered devices)</param>
/// <param name="CreatedAt">Creation timestamp</param>
public record NodeDto(
    Guid Id,
    Guid HubId,
    string NodeId,
    string Name,
    ProtocolDto Protocol,
    LocationDto? Location,
    IEnumerable<SensorDto> Sensors,
    DateTime? LastSeen,
    bool IsOnline,
    string? FirmwareVersion,
    int? BatteryLevel,
    DateTime CreatedAt
);

/// <summary>
/// DTO for creating a Node
/// </summary>
/// <param name="NodeId">Node identifier (e.g., "wetterstation-garten-01")</param>
/// <param name="Name">Display name (optional, will be generated from NodeId)</param>
/// <param name="HubIdentifier">Hub identifier string (e.g., "hub-home-01") - for auto-registration</param>
/// <param name="HubId">Hub ID (FK) - if using internal Guid</param>
/// <param name="Protocol">Communication protocol</param>
/// <param name="Location">Physical location</param>
/// <param name="Sensors">List of sensors to create</param>
public record CreateNodeDto(
    string NodeId,
    string? Name = null,
    string? HubIdentifier = null,
    Guid? HubId = null,
    ProtocolDto Protocol = ProtocolDto.WLAN,
    LocationDto? Location = null,
    IEnumerable<CreateSensorDto>? Sensors = null
);

/// <summary>
/// DTO for updating a Node
/// </summary>
/// <param name="Name">New display name</param>
/// <param name="Location">New location</param>
/// <param name="FirmwareVersion">New firmware version</param>
public record UpdateNodeDto(
    string? Name = null,
    LocationDto? Location = null,
    string? FirmwareVersion = null
);

/// <summary>
/// DTO for Node status updates
/// </summary>
/// <param name="NodeId">Node ID</param>
/// <param name="IsOnline">Online status</param>
/// <param name="LastSeen">Last seen timestamp</param>
/// <param name="BatteryLevel">Battery level</param>
public record NodeStatusDto(
    Guid NodeId,
    bool IsOnline,
    DateTime? LastSeen,
    int? BatteryLevel
);
