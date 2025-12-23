using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// BLE GATT Client Service for bidirectional communication with ESP32 sensors.
/// Sprint BT-01: Bluetooth Infrastructure
///
/// Authentication Flow:
/// 1. Connect to ESP32 by MAC address
/// 2. Read device info (CONFIG_READ characteristic) to get node ID hash
/// 3. Send CMD_AUTH with the hash via CONFIG_WRITE
/// 4. On success, send config commands (WiFi, Hub URL, etc.)
/// </summary>
public interface IBleGattClientService
{
    /// <summary>
    /// GATT Service UUID for config exchange
    /// </summary>
    static readonly Guid ConfigServiceUuid = Guid.Parse("4d494f54-4752-4944-434f-4e4649470000");

    /// <summary>
    /// Characteristic UUID for writing config commands to ESP32
    /// </summary>
    static readonly Guid ConfigWriteCharUuid = Guid.Parse("4d494f54-4752-4944-434f-4e4649470001");

    /// <summary>
    /// Characteristic UUID for reading device info from ESP32
    /// </summary>
    static readonly Guid ConfigReadCharUuid = Guid.Parse("4d494f54-4752-4944-434f-4e4649470002");

    /// <summary>
    /// Characteristic UUID for reading sensor data from ESP32
    /// </summary>
    static readonly Guid SensorDataCharUuid = Guid.Parse("4d494f54-4752-4944-434f-4e4649470003");

    /// <summary>
    /// Connect to an ESP32 sensor by MAC address and read device info.
    /// </summary>
    /// <param name="macAddress">MAC address of the ESP32 (e.g., "00:70:07:84:92:CE")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Connection result with device info if successful</returns>
    Task<BleConnectionResultDto> ConnectAsync(string macAddress, CancellationToken ct = default);

    /// <summary>
    /// Disconnect from the currently connected ESP32.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Check if currently connected to an ESP32.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Get the MAC address of the currently connected device.
    /// </summary>
    string? ConnectedDeviceMac { get; }

    /// <summary>
    /// Authenticate with the connected ESP32 using the node ID hash.
    /// Must be called before sending any config commands.
    /// </summary>
    /// <param name="nodeIdHash">4-byte hash of the node ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<BleAuthResultDto> AuthenticateAsync(byte[] nodeIdHash, CancellationToken ct = default);

    /// <summary>
    /// Check if currently authenticated with the connected ESP32.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Send WiFi credentials to the ESP32.
    /// Requires authentication first.
    /// </summary>
    /// <param name="config">WiFi configuration (SSID and password)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Config result</returns>
    Task<BleConfigResultDto> SetWifiAsync(BleWifiConfigDto config, CancellationToken ct = default);

    /// <summary>
    /// Send Hub URL configuration to the ESP32.
    /// Requires authentication first.
    /// </summary>
    /// <param name="config">Hub URL configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Config result</returns>
    Task<BleConfigResultDto> SetHubUrlAsync(BleHubUrlConfigDto config, CancellationToken ct = default);

    /// <summary>
    /// Set the Node ID on the ESP32.
    /// Requires authentication first.
    /// </summary>
    /// <param name="nodeId">Node ID to set</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Config result</returns>
    Task<BleConfigResultDto> SetNodeIdAsync(string nodeId, CancellationToken ct = default);

    /// <summary>
    /// Set the sensor reading interval on the ESP32.
    /// Requires authentication first.
    /// </summary>
    /// <param name="intervalSeconds">Interval in seconds</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Config result</returns>
    Task<BleConfigResultDto> SetIntervalAsync(uint intervalSeconds, CancellationToken ct = default);

    /// <summary>
    /// Perform factory reset on the ESP32.
    /// Requires authentication first.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Config result</returns>
    Task<BleConfigResultDto> FactoryResetAsync(CancellationToken ct = default);

    /// <summary>
    /// Reboot the ESP32.
    /// Requires authentication first.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Config result</returns>
    Task<BleConfigResultDto> RebootAsync(CancellationToken ct = default);

    /// <summary>
    /// Read current sensor data from the ESP32.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current sensor readings</returns>
    Task<BleSensorDataDto?> ReadSensorDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Read device info from the ESP32.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Device info including node ID and firmware version</returns>
    Task<BleDeviceInfoDto?> ReadDeviceInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Compute the 4-byte hash of a node ID (same algorithm as ESP32).
    /// Used for authentication.
    /// </summary>
    /// <param name="nodeId">Node ID to hash</param>
    /// <returns>4-byte hash</returns>
    byte[] ComputeNodeIdHash(string nodeId);

    /// <summary>
    /// High-level method to connect and authenticate in one call.
    /// </summary>
    /// <param name="macAddress">MAC address of the ESP32</param>
    /// <param name="nodeId">Node ID for authentication</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<BleAuthResultDto> ConnectAndAuthenticateAsync(string macAddress, string nodeId, CancellationToken ct = default);

    /// <summary>
    /// High-level method to provision an ESP32 with all configuration.
    /// Connects, authenticates, and sends all config in one call.
    /// </summary>
    /// <param name="macAddress">MAC address of the ESP32</param>
    /// <param name="nodeId">Node ID for authentication and to set on device</param>
    /// <param name="wifiConfig">WiFi configuration</param>
    /// <param name="hubUrlConfig">Hub URL configuration</param>
    /// <param name="intervalSeconds">Sensor reading interval (optional)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Overall provisioning result</returns>
    Task<BleConfigResultDto> ProvisionDeviceAsync(
        string macAddress,
        string nodeId,
        BleWifiConfigDto wifiConfig,
        BleHubUrlConfigDto hubUrlConfig,
        uint? intervalSeconds = null,
        CancellationToken ct = default);
}
