namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for Hub status information
/// </summary>
/// <param name="IsOnline">Current online status (always true when API is reachable)</param>
/// <param name="LastSeen">Last heartbeat timestamp</param>
/// <param name="NodeCount">Number of connected nodes</param>
/// <param name="OnlineNodeCount">Number of online nodes</param>
/// <param name="Services">Individual service status</param>
public record HubStatusDto(
    bool IsOnline,
    DateTime? LastSeen,
    int NodeCount,
    int OnlineNodeCount,
    ServiceStatusDto Services
);

/// <summary>
/// Status of individual Hub services
/// </summary>
/// <param name="Api">Backend API status (always online if this response is received)</param>
/// <param name="Database">Database connection status</param>
/// <param name="Mqtt">MQTT broker connection status</param>
/// <param name="Cloud">Cloud connection status</param>
public record ServiceStatusDto(
    ServiceState Api,
    ServiceState Database,
    ServiceState Mqtt,
    ServiceState Cloud
);

/// <summary>
/// Individual service state
/// </summary>
/// <param name="IsOnline">Whether the service is online</param>
/// <param name="Message">Optional status message</param>
public record ServiceState(
    bool IsOnline,
    string? Message = null
);
