namespace myIoTGrid.Shared.Common.Options;

/// <summary>
/// Configuration options for Matter Bridge integration
/// </summary>
public class MatterBridgeOptions
{
    public const string SectionName = "MatterBridge";

    /// <summary>
    /// Enable/disable Matter Bridge integration
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Base URL of the Matter Bridge API (e.g., http://localhost:3000)
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>
    /// HTTP request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Sensor types that should be registered as Matter devices
    /// </summary>
    public string[] EnabledSensorTypes { get; set; } = ["temperature", "humidity", "pressure"];

    /// <summary>
    /// Enable alerts as Contact Sensors in Matter
    /// </summary>
    public bool EnableAlertSensors { get; set; } = true;
}
