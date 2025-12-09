namespace myIoTGrid.Shared.Common.Options;

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
    /// Interval in seconds between node checks (default: 60 seconds)
    /// </summary>
    public int NodeCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Time in minutes after which a node is considered offline (default: 5 minutes)
    /// </summary>
    public int NodeOfflineTimeoutMinutes { get; set; } = 5;

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
    /// Number of days to retain reading data (default: 30 days)
    /// </summary>
    public int DataRetentionDays { get; set; } = 30;

    /// <summary>
    /// Whether to enable node monitoring (default: true)
    /// </summary>
    public bool EnableNodeMonitoring { get; set; } = true;

    /// <summary>
    /// Whether to enable hub monitoring (default: true)
    /// </summary>
    public bool EnableHubMonitoring { get; set; } = true;

    /// <summary>
    /// Whether to enable data retention cleanup (default: true)
    /// </summary>
    public bool EnableDataRetention { get; set; } = true;

    // === Remote Debug System (Sprint 8) ===

    /// <summary>
    /// Whether to enable debug log cleanup (default: true)
    /// </summary>
    public bool EnableDebugLogCleanup { get; set; } = true;

    /// <summary>
    /// Interval in hours between debug log cleanup runs (default: 6 hours)
    /// </summary>
    public int DebugLogCleanupIntervalHours { get; set; } = 6;

    /// <summary>
    /// Number of days to retain debug logs (default: 7 days)
    /// </summary>
    public int DebugLogRetentionDays { get; set; } = 7;

    /// <summary>
    /// Maximum number of debug logs per node (default: 10000)
    /// </summary>
    public int MaxDebugLogsPerNode { get; set; } = 10000;
}
