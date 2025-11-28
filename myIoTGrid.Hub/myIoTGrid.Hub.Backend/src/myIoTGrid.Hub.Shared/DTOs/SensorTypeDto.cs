namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for SensorType information.
/// Matter-konform: Entspricht einem Matter Cluster.
/// </summary>
/// <param name="TypeId">Primary key (e.g., "temperature")</param>
/// <param name="DisplayName">Display name (e.g., "Temperatur")</param>
/// <param name="ClusterId">Matter Cluster ID (0x0402 = TemperatureMeasurement)</param>
/// <param name="MatterClusterName">Matter Cluster Name (e.g., "TemperatureMeasurement")</param>
/// <param name="Unit">Unit (e.g., "°C")</param>
/// <param name="Resolution">Resolution (e.g., 0.1)</param>
/// <param name="MinValue">Minimum value</param>
/// <param name="MaxValue">Maximum value</param>
/// <param name="Description">Description</param>
/// <param name="IsCustom">Is this a custom myIoTGrid type?</param>
/// <param name="Category">Category (weather, water, air, soil, other)</param>
/// <param name="Icon">Material Icon Name</param>
/// <param name="Color">Hex Color for UI</param>
/// <param name="IsGlobal">Whether this type is global (defined by Cloud)</param>
/// <param name="CreatedAt">Creation timestamp</param>
public record SensorTypeDto(
    string TypeId,
    string DisplayName,
    uint ClusterId,
    string? MatterClusterName,
    string Unit,
    double Resolution,
    double? MinValue,
    double? MaxValue,
    string? Description,
    bool IsCustom,
    string Category,
    string? Icon,
    string? Color,
    bool IsGlobal,
    DateTime CreatedAt
);

/// <summary>
/// DTO for creating a SensorType
/// </summary>
/// <param name="TypeId">Primary key (e.g., "temperature")</param>
/// <param name="DisplayName">Display name (e.g., "Temperatur")</param>
/// <param name="ClusterId">Matter Cluster ID</param>
/// <param name="Unit">Unit (e.g., "°C")</param>
/// <param name="MatterClusterName">Matter Cluster Name</param>
/// <param name="Resolution">Resolution</param>
/// <param name="MinValue">Minimum value</param>
/// <param name="MaxValue">Maximum value</param>
/// <param name="Description">Description</param>
/// <param name="IsCustom">Is this a custom type?</param>
/// <param name="Category">Category</param>
/// <param name="Icon">Material Icon Name</param>
/// <param name="Color">Hex Color</param>
public record CreateSensorTypeDto(
    string TypeId,
    string DisplayName,
    uint ClusterId,
    string Unit,
    string? MatterClusterName = null,
    double Resolution = 0.1,
    double? MinValue = null,
    double? MaxValue = null,
    string? Description = null,
    bool IsCustom = false,
    string Category = "other",
    string? Icon = null,
    string? Color = null
);
