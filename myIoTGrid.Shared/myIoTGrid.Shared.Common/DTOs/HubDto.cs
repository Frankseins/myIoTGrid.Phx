namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for Hub (Raspberry Pi Gateway) information
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="TenantId">Tenant-ID</param>
/// <param name="HubId">Hub identifier (e.g., "hub-home-01")</param>
/// <param name="Name">Display name</param>
/// <param name="Description">Optional description</param>
/// <param name="LastSeen">Last heartbeat</param>
/// <param name="IsOnline">Online status</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="SensorCount">Number of connected sensors</param>
/// <param name="DefaultWifiSsid">Default WiFi SSID for node provisioning</param>
/// <param name="DefaultWifiPassword">Default WiFi password for node provisioning</param>
/// <param name="ApiUrl">API URL for nodes to connect to</param>
/// <param name="ApiPort">API Port (default 5002)</param>
public record HubDto(
    Guid Id,
    Guid TenantId,
    string HubId,
    string Name,
    string? Description,
    DateTime? LastSeen,
    bool IsOnline,
    DateTime CreatedAt,
    int SensorCount,
    string? DefaultWifiSsid = null,
    string? DefaultWifiPassword = null,
    string? ApiUrl = null,
    int ApiPort = 5002
);

/// <summary>
/// DTO for creating a Hub
/// </summary>
/// <param name="HubId">Hub identifier (e.g., "hub-home-01")</param>
/// <param name="Name">Display name (optional, will be generated from HubId)</param>
/// <param name="Description">Optional description</param>
public record CreateHubDto(
    string HubId,
    string? Name = null,
    string? Description = null
);

/// <summary>
/// DTO for updating a Hub
/// </summary>
/// <param name="Name">New display name</param>
/// <param name="Description">New description</param>
/// <param name="DefaultWifiSsid">Default WiFi SSID for node provisioning</param>
/// <param name="DefaultWifiPassword">Default WiFi password for node provisioning</param>
/// <param name="ApiUrl">API URL for nodes to connect to</param>
/// <param name="ApiPort">API Port (default 5002)</param>
public record UpdateHubDto(
    string? Name = null,
    string? Description = null,
    string? DefaultWifiSsid = null,
    string? DefaultWifiPassword = null,
    string? ApiUrl = null,
    int? ApiPort = null
);

/// <summary>
/// DTO for Hub provisioning settings (for BLE setup wizard)
/// </summary>
/// <param name="DefaultWifiSsid">Default WiFi SSID</param>
/// <param name="DefaultWifiPassword">Default WiFi password</param>
/// <param name="ApiUrl">API URL for nodes</param>
/// <param name="ApiPort">API Port</param>
public record HubProvisioningSettingsDto(
    string? DefaultWifiSsid,
    string? DefaultWifiPassword,
    string? ApiUrl,
    int ApiPort
);

/// <summary>
/// DTO for BLE provisioning data sent to sensor node
/// Contains everything needed to configure a new sensor via BLE
/// </summary>
/// <param name="WifiSsid">WiFi network name</param>
/// <param name="WifiPassword">WiFi password</param>
/// <param name="ApiUrl">Hub API URL (e.g., "http://192.168.1.100:5002")</param>
/// <param name="NodeId">Assigned Node ID (GUID)</param>
/// <param name="NodeName">Display name for the node</param>
/// <param name="ApiKey">API key for authentication</param>
public record BleProvisioningDataDto(
    string WifiSsid,
    string WifiPassword,
    string ApiUrl,
    Guid NodeId,
    string NodeName,
    string ApiKey
);
