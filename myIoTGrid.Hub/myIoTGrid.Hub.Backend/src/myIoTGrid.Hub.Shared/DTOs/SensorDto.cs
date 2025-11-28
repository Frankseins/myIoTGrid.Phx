namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for Sensor (physical sensor chip: DHT22, BME280, SCD40) information.
/// Matter-konform: Entspricht einem Matter Endpoint.
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="NodeId">Node ID (FK)</param>
/// <param name="SensorTypeId">Sensor type ID (e.g., "temperature")</param>
/// <param name="EndpointId">Matter Endpoint ID (1, 2, 3...)</param>
/// <param name="Name">Optional display name for this sensor</param>
/// <param name="IsActive">Whether this sensor is active</param>
/// <param name="SensorType">Sensor type details</param>
/// <param name="CreatedAt">Creation timestamp</param>
public record SensorDto(
    Guid Id,
    Guid NodeId,
    string SensorTypeId,
    int EndpointId,
    string? Name,
    bool IsActive,
    SensorTypeDto? SensorType,
    DateTime CreatedAt
);

/// <summary>
/// DTO for creating a Sensor on a Node
/// </summary>
/// <param name="SensorTypeId">Sensor type ID (e.g., "temperature")</param>
/// <param name="EndpointId">Matter Endpoint ID (1, 2, 3...)</param>
/// <param name="Name">Optional display name</param>
public record CreateSensorDto(
    string SensorTypeId,
    int EndpointId,
    string? Name = null
);

/// <summary>
/// DTO for updating a Sensor
/// </summary>
/// <param name="Name">New display name</param>
/// <param name="IsActive">Whether this sensor is active</param>
public record UpdateSensorDto(
    string? Name = null,
    bool? IsActive = null
);
