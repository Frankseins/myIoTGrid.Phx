namespace myIoTGrid.Shared.Common.DTOs.Chart;

/// <summary>
/// Time intervals for chart data aggregation.
/// </summary>
public enum ChartInterval
{
    /// <summary>Last hour - points every 1 minute (max 60)</summary>
    OneHour = 0,
    /// <summary>Last day - points every 15 minutes (max 96)</summary>
    OneDay = 1,
    /// <summary>Last week - points every 1 hour (max 168)</summary>
    OneWeek = 2,
    /// <summary>Last month - points every 6 hours (max 124)</summary>
    OneMonth = 3,
    /// <summary>Last 3 months - points every 1 day (max 92)</summary>
    ThreeMonths = 4,
    /// <summary>Last 6 months - points every 1 day (max 183)</summary>
    SixMonths = 5,
    /// <summary>Last year - points every 1 week (max 52)</summary>
    OneYear = 6
}

/// <summary>
/// Complete chart data for widget detail view.
/// </summary>
public record ChartDataDto(
    /// <summary>Node ID</summary>
    Guid NodeId,
    /// <summary>Node name</summary>
    string NodeName,
    /// <summary>Sensor assignment ID</summary>
    Guid? AssignmentId,
    /// <summary>Sensor ID</summary>
    Guid? SensorId,
    /// <summary>Sensor name (e.g., "DHT22")</summary>
    string SensorName,
    /// <summary>Measurement type (e.g., "temperature")</summary>
    string MeasurementType,
    /// <summary>Location name</summary>
    string LocationName,
    /// <summary>Unit of measurement</summary>
    string Unit,
    /// <summary>Sensor color</summary>
    string Color,
    /// <summary>Current/latest value</summary>
    double CurrentValue,
    /// <summary>Last update timestamp</summary>
    DateTime LastUpdate,
    /// <summary>Statistics for the period</summary>
    ChartStatsDto Stats,
    /// <summary>Trend compared to previous period</summary>
    TrendDto Trend,
    /// <summary>Chart data points</summary>
    IEnumerable<ChartPointDto> DataPoints
);

/// <summary>
/// Statistics for a chart period.
/// </summary>
public record ChartStatsDto(
    /// <summary>Minimum value in period</summary>
    double MinValue,
    /// <summary>Timestamp of minimum value</summary>
    DateTime MinTimestamp,
    /// <summary>Maximum value in period</summary>
    double MaxValue,
    /// <summary>Timestamp of maximum value</summary>
    DateTime MaxTimestamp,
    /// <summary>Average value in period</summary>
    double AvgValue
);

/// <summary>
/// Trend information comparing current value to previous period.
/// </summary>
public record TrendDto(
    /// <summary>Absolute change (e.g., +2.3)</summary>
    double Change,
    /// <summary>Percentage change (e.g., +9.8)</summary>
    double ChangePercent,
    /// <summary>Direction: "up", "down", or "stable"</summary>
    string Direction
);

/// <summary>
/// Single data point for chart.
/// </summary>
public record ChartPointDto(
    /// <summary>Timestamp of the data point</summary>
    DateTime Timestamp,
    /// <summary>Value at this point</summary>
    double Value,
    /// <summary>Minimum value in aggregation interval (optional)</summary>
    double? Min,
    /// <summary>Maximum value in aggregation interval (optional)</summary>
    double? Max
);

/// <summary>
/// Request for paginated readings list.
/// </summary>
public record ReadingsListRequestDto(
    int Page = 1,
    int PageSize = 20,
    DateTime? From = null,
    DateTime? To = null
);

/// <summary>
/// Single reading for the list view.
/// </summary>
public record ReadingListItemDto(
    long Id,
    DateTime Timestamp,
    double Value,
    string Unit,
    string? TrendDirection
);

/// <summary>
/// Paginated readings list response.
/// </summary>
public record ReadingsListDto(
    IEnumerable<ReadingListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
