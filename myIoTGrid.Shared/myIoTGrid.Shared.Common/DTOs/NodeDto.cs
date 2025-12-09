using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.DTOs;

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
    DateTime CreatedAt,
    string MacAddress,
    NodeProvisioningStatusDto Status,
    bool IsSimulation,
    // Sprint OS-01: Offline Storage
    StorageModeDto StorageMode,
    int PendingSyncCount,
    DateTime? LastSyncAt,
    string? LastSyncError,
    // Sprint 8: Remote Debug System
    DebugLevelDto DebugLevel,
    bool EnableRemoteLogging,
    DateTime? LastDebugChange
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
    string? FirmwareVersion = null,
    bool? IsSimulation = null,
    // Sprint OS-01: Offline Storage
    StorageModeDto? StorageMode = null
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

// === Node Provisioning DTOs ===

/// <summary>
/// DTO for node registration request (from ESP32 via BLE).
/// Sent when node first connects to Hub.
/// </summary>
public record NodeRegistrationDto(
    string MacAddress,
    string? FirmwareVersion = null,
    string? Name = null
);

/// <summary>
/// DTO for node configuration response (sent to ESP32 via BLE).
/// Contains WiFi credentials and API key for node to connect.
/// </summary>
public record NodeConfigurationDto(
    string NodeId,
    string ApiKey,
    string WifiSsid,
    string WifiPassword,
    string HubApiUrl
);

/// <summary>
/// DTO for node heartbeat request.
/// Sent periodically by node to Hub via REST API.
/// </summary>
public record NodeHeartbeatDto(
    string NodeId,
    string? FirmwareVersion = null,
    int? BatteryLevel = null
);

/// <summary>
/// DTO for node heartbeat response.
/// Returned by Hub to node.
/// </summary>
public record NodeHeartbeatResponseDto(
    bool Success,
    DateTime ServerTime,
    int? NextHeartbeatSeconds = null
);

/// <summary>
/// DTO for node sensor configuration response.
/// Returns full sensor configuration for the node to start measuring.
/// </summary>
public record NodeSensorConfigurationDto(
    Guid NodeId,
    string SerialNumber,
    string Name,
    bool IsSimulation,
    int DefaultIntervalSeconds,
    List<SensorAssignmentConfigDto> Sensors,
    DateTime ConfigurationTimestamp
);

/// <summary>
/// Individual sensor assignment configuration for a node.
/// Contains all pin and timing configuration for the sensor.
/// </summary>
public record SensorAssignmentConfigDto(
    int EndpointId,
    string SensorCode,
    string SensorName,
    string? Icon,
    string? Color,
    bool IsActive,
    int IntervalSeconds,
    string? I2CAddress,
    int? SdaPin,
    int? SclPin,
    int? OneWirePin,
    int? AnalogPin,
    int? DigitalPin,
    int? TriggerPin,
    int? EchoPin,
    double OffsetCorrection,
    double GainCorrection,
    List<SensorCapabilityConfigDto> Capabilities
);

/// <summary>
/// Sensor capability configuration for the firmware.
/// Tells the sensor which measurement types to capture and their units.
/// </summary>
public record SensorCapabilityConfigDto(
    string MeasurementType,
    string DisplayName,
    string Unit
);

// === GPS Status DTOs ===

/// <summary>
/// GPS status aggregated from latest readings.
/// Provides fix quality, satellite count, and position data.
/// </summary>
public record NodeGpsStatusDto(
    Guid NodeId,
    string NodeName,
    bool HasGps,
    int Satellites,
    int FixType,
    string FixTypeText,
    double Hdop,
    string HdopQuality,
    double? Latitude,
    double? Longitude,
    double? Altitude,
    double? Speed,
    DateTime? LastUpdate
);

/// <summary>
/// GPS position data
/// </summary>
public record GpsPositionDto(
    double Latitude,
    double Longitude,
    double? Altitude,
    double? Speed,
    DateTime Timestamp
);
