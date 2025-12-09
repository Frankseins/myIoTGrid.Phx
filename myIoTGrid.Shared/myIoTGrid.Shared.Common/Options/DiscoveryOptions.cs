namespace myIoTGrid.Shared.Common.Options;

/// <summary>
/// Configuration options for the UDP discovery service.
/// Allows sensors to find the hub on the local network via UDP broadcast.
/// </summary>
public class DiscoveryOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Discovery";

    /// <summary>
    /// Whether the discovery service is enabled (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// UDP port to listen on for discovery requests (default: 5001)
    /// </summary>
    public int Port { get; set; } = 5001;

    /// <summary>
    /// Hub identifier to announce (will use system hub ID if not set)
    /// </summary>
    public string? HubId { get; set; }

    /// <summary>
    /// Hub display name to announce (will use system hub name if not set)
    /// </summary>
    public string? HubName { get; set; }

    /// <summary>
    /// Protocol for API URL (http or https, default: https)
    /// </summary>
    public string Protocol { get; set; } = "https";

    /// <summary>
    /// API port to include in the response URL (default: 5001)
    /// </summary>
    public int ApiPort { get; set; } = 5001;

    /// <summary>
    /// Optional specific IP address to announce. If not set, all local IPs will be tried.
    /// </summary>
    public string? AdvertiseIp { get; set; }

    /// <summary>
    /// Network interface to bind to (e.g., "eth0", "wlan0"). Empty means all interfaces.
    /// </summary>
    public string? NetworkInterface { get; set; }

    /// <summary>
    /// Timeout in milliseconds for receiving discovery requests (default: 1000ms)
    /// </summary>
    public int ReceiveTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Whether to log discovery requests (useful for debugging, default: false)
    /// </summary>
    public bool LogDiscoveryRequests { get; set; } = false;
}
