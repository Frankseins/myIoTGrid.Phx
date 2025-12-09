namespace myIoTGrid.Shared.Common.DTOs.Dashboard;

/// <summary>
/// DTO for the complete dashboard with locations and their sensor widgets.
/// Groups sensor data by location for Home Assistant-style dashboard.
/// </summary>
public record LocationDashboardDto(
    IEnumerable<LocationGroupDto> Locations
);

/// <summary>
/// DTO for a location group with its sensor widgets.
/// </summary>
public record LocationGroupDto(
    /// <summary>Name of the location (e.g., "Außen", "Wohnzimmer")</summary>
    string LocationName,
    /// <summary>Icon for the location (emoji)</summary>
    string? LocationIcon,
    /// <summary>Whether this location should be displayed as hero widget (full width)</summary>
    bool IsHero,
    /// <summary>Sensor widgets for this location</summary>
    IEnumerable<SensorWidgetDto> Widgets
);

/// <summary>
/// DTO for a single sensor widget with sparkline data.
/// </summary>
public record SensorWidgetDto(
    /// <summary>Unique widget ID for frontend tracking</summary>
    string WidgetId,
    /// <summary>Node ID</summary>
    Guid NodeId,
    /// <summary>Node name</summary>
    string NodeName,
    /// <summary>Sensor assignment ID</summary>
    Guid? AssignmentId,
    /// <summary>Sensor ID</summary>
    Guid? SensorId,
    /// <summary>Measurement type (e.g., "temperature")</summary>
    string MeasurementType,
    /// <summary>Sensor name (e.g., "DHT22", "BME280")</summary>
    string SensorName,
    /// <summary>Location name (e.g., "Wohnzimmer")</summary>
    string LocationName,
    /// <summary>Display label (deprecated - use SensorName and LocationName)</summary>
    string Label,
    /// <summary>Unit of measurement (e.g., "°C")</summary>
    string Unit,
    /// <summary>Hex color for sparkline (e.g., "#FF5722")</summary>
    string Color,
    /// <summary>Current/latest value</summary>
    double CurrentValue,
    /// <summary>Timestamp of last update</summary>
    DateTime LastUpdate,
    /// <summary>Min/Max values with timestamps</summary>
    MinMaxDto MinMax,
    /// <summary>Sparkline data points</summary>
    IEnumerable<SparklinePointDto> DataPoints
);

/// <summary>
/// DTO for min/max values with timestamps.
/// </summary>
public record MinMaxDto(
    double MinValue,
    DateTime MinTimestamp,
    double MaxValue,
    DateTime MaxTimestamp
);

/// <summary>
/// DTO for a single sparkline data point.
/// </summary>
public record SparklinePointDto(
    DateTime Timestamp,
    double Value
);

/// <summary>
/// Time period for sparkline data.
/// </summary>
public enum SparklinePeriod
{
    Hour = 0,
    Day = 1,
    Week = 2
}

/// <summary>
/// DTO for dashboard filter options.
/// </summary>
public record DashboardFilterOptionsDto(
    /// <summary>Available locations for filtering</summary>
    IEnumerable<string> Locations,
    /// <summary>Available measurement types for filtering</summary>
    IEnumerable<string> MeasurementTypes
);

/// <summary>
/// DTO for dashboard filter request.
/// </summary>
public record DashboardFilterDto(
    /// <summary>Filter by locations (empty = all)</summary>
    string[]? Locations = null,
    /// <summary>Filter by measurement types (empty = all)</summary>
    string[]? MeasurementTypes = null,
    /// <summary>Time period for sparkline data</summary>
    SparklinePeriod Period = SparklinePeriod.Day
);
