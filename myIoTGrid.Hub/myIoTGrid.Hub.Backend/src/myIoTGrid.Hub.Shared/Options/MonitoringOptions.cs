namespace myIoTGrid.Hub.Shared.Options;

/// <summary>
/// Configuration options for monitoring services
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Monitoring";

    /// <summary>
    /// Interval in seconds between sensor checks (default: 60 seconds)
    /// </summary>
    public int SensorCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Time in minutes after which a sensor is considered offline (default: 5 minutes)
    /// </summary>
    public int SensorOfflineTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Interval in seconds between hub checks (default: 60 seconds)
    /// </summary>
    public int HubCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Time in minutes after which a hub is considered offline (default: 5 minutes)
    /// </summary>
    public int HubOfflineTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Interval in hours between data retention cleanup runs (default: 24 hours)
    /// </summary>
    public int DataRetentionIntervalHours { get; set; } = 24;

    /// <summary>
    /// Number of days to retain sensor data (default: 30 days)
    /// </summary>
    public int DataRetentionDays { get; set; } = 30;

    /// <summary>
    /// Whether to enable sensor monitoring (default: true)
    /// </summary>
    public bool EnableSensorMonitoring { get; set; } = true;

    /// <summary>
    /// Whether to enable hub monitoring (default: true)
    /// </summary>
    public bool EnableHubMonitoring { get; set; } = true;

    /// <summary>
    /// Whether to enable data retention cleanup (default: true)
    /// </summary>
    public bool EnableDataRetention { get; set; } = true;
}
