using System.Text;
using System.Text.Json;
using InTheHand.Bluetooth;
using Microsoft.Extensions.Logging;
using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Contracts.Services;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// BLE GATT Client Service for bidirectional communication with ESP32 sensors.
/// Uses InTheHand.BluetoothLE for cross-platform BLE support (Linux via BlueZ).
/// Sprint BT-01: Bluetooth Infrastructure
/// </summary>
public class BleGattClientService : IBleGattClientService, IDisposable
{
    private readonly ILogger<BleGattClientService> _logger;

    // GATT UUIDs (matching ESP32 firmware)
    private static readonly BluetoothUuid ConfigServiceUuid = BluetoothUuid.FromGuid(
        Guid.Parse("4d494f54-4752-4944-434f-4e4649470000"));
    private static readonly BluetoothUuid ConfigWriteCharUuid = BluetoothUuid.FromGuid(
        Guid.Parse("4d494f54-4752-4944-434f-4e4649470001"));
    private static readonly BluetoothUuid ConfigReadCharUuid = BluetoothUuid.FromGuid(
        Guid.Parse("4d494f54-4752-4944-434f-4e4649470002"));
    private static readonly BluetoothUuid SensorDataCharUuid = BluetoothUuid.FromGuid(
        Guid.Parse("4d494f54-4752-4944-434f-4e4649470003"));

    // Connection state
    private BluetoothDevice? _device;
    private RemoteGattServer? _gattServer;
    private GattService? _configService;
    private GattCharacteristic? _configWriteChar;
    private GattCharacteristic? _configReadChar;
    private GattCharacteristic? _sensorDataChar;

    private bool _isAuthenticated;
    private string? _connectedMac;
    private BleDeviceInfoDto? _deviceInfo;

    public BleGattClientService(ILogger<BleGattClientService> logger)
    {
        _logger = logger;
    }

    public bool IsConnected => _gattServer?.IsConnected ?? false;
    public string? ConnectedDeviceMac => _connectedMac;
    public bool IsAuthenticated => _isAuthenticated && IsConnected;

    public async Task<BleConnectionResultDto> ConnectAsync(string macAddress, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Connecting to BLE device {MacAddress}...", macAddress);

            // Disconnect any existing connection
            await DisconnectAsync();

            // Parse MAC address
            var deviceAddress = ulong.Parse(macAddress.Replace(":", ""), System.Globalization.NumberStyles.HexNumber);

            // Get or request device
            _device = await GetDeviceByAddressAsync(macAddress, ct);
            if (_device == null)
            {
                return new BleConnectionResultDto(false, macAddress, null, null,
                    "Device not found. Make sure it's advertising.");
            }

            _connectedMac = macAddress;
            var deviceName = _device.Name ?? $"myIoTGrid-{macAddress[^2..]}";

            // Connect to GATT server
            _logger.LogDebug("Connecting to GATT server...");
            _gattServer = _device.Gatt;
            await _gattServer.ConnectAsync();

            if (!_gattServer.IsConnected)
            {
                return new BleConnectionResultDto(false, macAddress, deviceName, null,
                    "Failed to connect to GATT server");
            }

            _logger.LogDebug("Connected to GATT server. Discovering services...");

            // Get config service
            _configService = await _gattServer.GetPrimaryServiceAsync(ConfigServiceUuid);
            if (_configService == null)
            {
                _logger.LogWarning("Config service not found on device {MacAddress}", macAddress);
                return new BleConnectionResultDto(false, macAddress, deviceName, null,
                    "Config service not found. Is this a myIoTGrid device?");
            }

            // Get characteristics
            _configWriteChar = await _configService.GetCharacteristicAsync(ConfigWriteCharUuid);
            _configReadChar = await _configService.GetCharacteristicAsync(ConfigReadCharUuid);
            _sensorDataChar = await _configService.GetCharacteristicAsync(SensorDataCharUuid);

            if (_configWriteChar == null || _configReadChar == null)
            {
                _logger.LogWarning("Required characteristics not found on device {MacAddress}", macAddress);
                return new BleConnectionResultDto(false, macAddress, deviceName, null,
                    "Required characteristics not found");
            }

            _logger.LogDebug("Characteristics discovered. Reading device info...");

            // Read device info
            _deviceInfo = await ReadDeviceInfoAsync(ct);

            _logger.LogInformation("Connected to {DeviceName} ({MacAddress}). Firmware: {Firmware}",
                deviceName, macAddress, _deviceInfo?.FirmwareVersion ?? "unknown");

            return new BleConnectionResultDto(true, macAddress, deviceName, _deviceInfo,
                "Connected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to BLE device {MacAddress}", macAddress);
            await DisconnectAsync();
            return new BleConnectionResultDto(false, macAddress, null, null, ex.Message);
        }
    }

    private async Task<BluetoothDevice?> GetDeviceByAddressAsync(string macAddress, CancellationToken ct)
    {
        // First, try to find device via scan
        _logger.LogDebug("Scanning for device {MacAddress}...", macAddress);

        try
        {
            // First check paired devices
            var pairedDevices = await Bluetooth.GetPairedDevicesAsync();
            foreach (var device in pairedDevices)
            {
                var deviceMac = FormatMacAddress(device.Id);
                if (deviceMac.Equals(macAddress, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Found target device in paired devices: {Name} ({Mac})", device.Name, deviceMac);
                    return device;
                }
            }

            // If not paired, scan for devices using ScanForDevicesAsync
            _logger.LogDebug("Device not paired, scanning for advertisements...");

            using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            scanCts.CancelAfter(TimeSpan.FromSeconds(15));

            var scannedDevices = await Bluetooth.ScanForDevicesAsync();

            foreach (var device in scannedDevices)
            {
                var deviceMac = FormatMacAddress(device.Id);
                _logger.LogDebug("Found device: {Name} ({Mac})", device.Name ?? "(unknown)", deviceMac);

                if (deviceMac.Equals(macAddress, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Found target device {Name} ({Mac})", device.Name, deviceMac);
                    return device;
                }
            }

            _logger.LogWarning("Device {MacAddress} not found in {Count} scanned devices",
                macAddress, scannedDevices.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Scan cancelled or timeout reached");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for BLE device {MacAddress}", macAddress);
        }

        return null;
    }

    private static string FormatMacAddress(string id)
    {
        // Convert device ID to MAC address format (XX:XX:XX:XX:XX:XX)
        if (id.Contains(':'))
            return id.ToUpperInvariant();

        // Remove any non-hex characters and format
        var hex = new string(id.Where(c => char.IsLetterOrDigit(c)).ToArray());
        if (hex.Length >= 12)
        {
            return string.Join(":", Enumerable.Range(0, 6)
                .Select(i => hex.Substring(i * 2, 2).ToUpperInvariant()));
        }

        return id;
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_gattServer?.IsConnected == true)
            {
                _logger.LogDebug("Disconnecting from {MacAddress}...", _connectedMac);
                _gattServer.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during disconnect");
        }
        finally
        {
            _configWriteChar = null;
            _configReadChar = null;
            _sensorDataChar = null;
            _configService = null;
            _gattServer = null;
            _device = null;
            _isAuthenticated = false;
            _connectedMac = null;
            _deviceInfo = null;
        }
    }

    public async Task<BleAuthResultDto> AuthenticateAsync(byte[] nodeIdHash, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            return new BleAuthResultDto(false, _connectedMac ?? "", BleResponseCode.Error,
                "Not connected to device");
        }

        try
        {
            _logger.LogDebug("Authenticating with hash: {Hash}",
                BitConverter.ToString(nodeIdHash).Replace("-", ""));

            // Build auth command: CMD_AUTH (0x00) + 4-byte hash
            var command = new byte[1 + nodeIdHash.Length];
            command[0] = (byte)BleConfigCommand.Authenticate;
            Array.Copy(nodeIdHash, 0, command, 1, nodeIdHash.Length);

            // Write to config characteristic
            await _configWriteChar!.WriteValueWithResponseAsync(command);

            // Wait briefly for ESP32 to process
            await Task.Delay(100, ct);

            // Read response
            var response = await _configWriteChar.ReadValueAsync();
            var responseCode = response.Length > 0 ? (BleResponseCode)response[0] : BleResponseCode.Error;

            if (responseCode == BleResponseCode.Ok)
            {
                _isAuthenticated = true;
                _logger.LogInformation("Authentication successful for {MacAddress}", _connectedMac);
                return new BleAuthResultDto(true, _connectedMac!, responseCode, "Authentication successful");
            }
            else
            {
                _logger.LogWarning("Authentication failed for {MacAddress}: {Code}", _connectedMac, responseCode);
                return new BleAuthResultDto(false, _connectedMac!, responseCode,
                    $"Authentication failed: {responseCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication with {MacAddress}", _connectedMac);
            return new BleAuthResultDto(false, _connectedMac ?? "", BleResponseCode.Error, ex.Message);
        }
    }

    public async Task<BleConfigResultDto> SetWifiAsync(BleWifiConfigDto config, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return new BleConfigResultDto(false, BleConfigCommand.SetWifi, BleResponseCode.NotAuthenticated,
                "Not authenticated. Call AuthenticateAsync first.");
        }

        try
        {
            _logger.LogDebug("Setting WiFi SSID: {Ssid}", config.Ssid);

            // Build command: CMD_SET_WIFI (0x01) + SSID_LEN (1 byte) + SSID + PASSWORD_LEN (1 byte) + PASSWORD
            var ssidBytes = Encoding.UTF8.GetBytes(config.Ssid);
            var passwordBytes = Encoding.UTF8.GetBytes(config.Password);

            var command = new byte[1 + 1 + ssidBytes.Length + 1 + passwordBytes.Length];
            var offset = 0;

            command[offset++] = (byte)BleConfigCommand.SetWifi;
            command[offset++] = (byte)ssidBytes.Length;
            Array.Copy(ssidBytes, 0, command, offset, ssidBytes.Length);
            offset += ssidBytes.Length;
            command[offset++] = (byte)passwordBytes.Length;
            Array.Copy(passwordBytes, 0, command, offset, passwordBytes.Length);

            return await SendConfigCommandAsync(command, BleConfigCommand.SetWifi, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting WiFi config");
            return new BleConfigResultDto(false, BleConfigCommand.SetWifi, BleResponseCode.Error, ex.Message);
        }
    }

    public async Task<BleConfigResultDto> SetHubUrlAsync(BleHubUrlConfigDto config, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return new BleConfigResultDto(false, BleConfigCommand.SetHubUrl, BleResponseCode.NotAuthenticated,
                "Not authenticated. Call AuthenticateAsync first.");
        }

        try
        {
            _logger.LogDebug("Setting Hub URL: {Url}:{Port}", config.HubUrl, config.Port);

            // Build command: CMD_SET_HUB_URL (0x02) + URL_LEN (1 byte) + URL + PORT (2 bytes, little-endian)
            var urlBytes = Encoding.UTF8.GetBytes(config.HubUrl);
            var command = new byte[1 + 1 + urlBytes.Length + 2];
            var offset = 0;

            command[offset++] = (byte)BleConfigCommand.SetHubUrl;
            command[offset++] = (byte)urlBytes.Length;
            Array.Copy(urlBytes, 0, command, offset, urlBytes.Length);
            offset += urlBytes.Length;
            command[offset++] = (byte)(config.Port & 0xFF);
            command[offset] = (byte)((config.Port >> 8) & 0xFF);

            return await SendConfigCommandAsync(command, BleConfigCommand.SetHubUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Hub URL");
            return new BleConfigResultDto(false, BleConfigCommand.SetHubUrl, BleResponseCode.Error, ex.Message);
        }
    }

    public async Task<BleConfigResultDto> SetNodeIdAsync(string nodeId, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return new BleConfigResultDto(false, BleConfigCommand.SetNodeId, BleResponseCode.NotAuthenticated,
                "Not authenticated. Call AuthenticateAsync first.");
        }

        try
        {
            _logger.LogDebug("Setting Node ID: {NodeId}", nodeId);

            var nodeIdBytes = Encoding.UTF8.GetBytes(nodeId);
            var command = new byte[1 + 1 + nodeIdBytes.Length];
            command[0] = (byte)BleConfigCommand.SetNodeId;
            command[1] = (byte)nodeIdBytes.Length;
            Array.Copy(nodeIdBytes, 0, command, 2, nodeIdBytes.Length);

            return await SendConfigCommandAsync(command, BleConfigCommand.SetNodeId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Node ID");
            return new BleConfigResultDto(false, BleConfigCommand.SetNodeId, BleResponseCode.Error, ex.Message);
        }
    }

    public async Task<BleConfigResultDto> SetIntervalAsync(uint intervalSeconds, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return new BleConfigResultDto(false, BleConfigCommand.SetInterval, BleResponseCode.NotAuthenticated,
                "Not authenticated. Call AuthenticateAsync first.");
        }

        try
        {
            _logger.LogDebug("Setting interval: {Interval} seconds", intervalSeconds);

            // Build command: CMD_SET_INTERVAL (0x04) + INTERVAL (4 bytes, little-endian)
            var command = new byte[5];
            command[0] = (byte)BleConfigCommand.SetInterval;
            BitConverter.TryWriteBytes(command.AsSpan(1), intervalSeconds);

            return await SendConfigCommandAsync(command, BleConfigCommand.SetInterval, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting interval");
            return new BleConfigResultDto(false, BleConfigCommand.SetInterval, BleResponseCode.Error, ex.Message);
        }
    }

    public async Task<BleConfigResultDto> FactoryResetAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return new BleConfigResultDto(false, BleConfigCommand.FactoryReset, BleResponseCode.NotAuthenticated,
                "Not authenticated. Call AuthenticateAsync first.");
        }

        try
        {
            _logger.LogWarning("Performing factory reset on {MacAddress}", _connectedMac);
            var command = new byte[] { (byte)BleConfigCommand.FactoryReset };
            return await SendConfigCommandAsync(command, BleConfigCommand.FactoryReset, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during factory reset");
            return new BleConfigResultDto(false, BleConfigCommand.FactoryReset, BleResponseCode.Error, ex.Message);
        }
    }

    public async Task<BleConfigResultDto> RebootAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return new BleConfigResultDto(false, BleConfigCommand.Reboot, BleResponseCode.NotAuthenticated,
                "Not authenticated. Call AuthenticateAsync first.");
        }

        try
        {
            _logger.LogInformation("Rebooting device {MacAddress}", _connectedMac);
            var command = new byte[] { (byte)BleConfigCommand.Reboot };
            var result = await SendConfigCommandAsync(command, BleConfigCommand.Reboot, ct);

            // Device will disconnect after reboot
            if (result.Success)
            {
                await DisconnectAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reboot");
            return new BleConfigResultDto(false, BleConfigCommand.Reboot, BleResponseCode.Error, ex.Message);
        }
    }

    private async Task<BleConfigResultDto> SendConfigCommandAsync(byte[] command, BleConfigCommand cmdType, CancellationToken ct)
    {
        await _configWriteChar!.WriteValueWithResponseAsync(command);

        // Wait for ESP32 to process
        await Task.Delay(100, ct);

        // Read response
        var response = await _configWriteChar.ReadValueAsync();
        var responseCode = response.Length > 0 ? (BleResponseCode)response[0] : BleResponseCode.Error;

        if (responseCode == BleResponseCode.Ok)
        {
            _logger.LogDebug("Command {Command} succeeded", cmdType);
            return new BleConfigResultDto(true, cmdType, responseCode, "Command successful");
        }
        else
        {
            _logger.LogWarning("Command {Command} failed: {Response}", cmdType, responseCode);
            return new BleConfigResultDto(false, cmdType, responseCode, $"Command failed: {responseCode}");
        }
    }

    public async Task<BleSensorDataDto?> ReadSensorDataAsync(CancellationToken ct = default)
    {
        if (!IsConnected || _sensorDataChar == null)
        {
            _logger.LogWarning("Cannot read sensor data: not connected or characteristic not available");
            return null;
        }

        try
        {
            var data = await _sensorDataChar.ReadValueAsync();
            var json = Encoding.UTF8.GetString(data);
            _logger.LogDebug("Sensor data JSON: {Json}", json);

            // Parse JSON format from ESP32:
            // {"t":21.50,"h":65.0,"p":1013.0,"b":3300,"f":0}
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var temperature = root.TryGetProperty("t", out var t) ? (float)t.GetDouble() : 0f;
            var humidity = root.TryGetProperty("h", out var h) ? (float)h.GetDouble() : 0f;
            var pressure = root.TryGetProperty("p", out var p) ? (float)p.GetDouble() : 0f;
            var battery = root.TryGetProperty("b", out var b) ? (ushort)b.GetInt32() : (ushort)0;

            return new BleSensorDataDto(temperature, humidity, pressure, battery, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading sensor data");
            return null;
        }
    }

    public async Task<BleDeviceInfoDto?> ReadDeviceInfoAsync(CancellationToken ct = default)
    {
        if (!IsConnected || _configReadChar == null)
        {
            _logger.LogWarning("Cannot read device info: not connected or characteristic not available");
            return null;
        }

        try
        {
            var data = await _configReadChar.ReadValueAsync();
            var json = Encoding.UTF8.GetString(data);
            _logger.LogDebug("Device info JSON: {Json}", json);

            // Parse JSON format from ESP32:
            // {"nodeId":"...","deviceName":"...","firmware":"...","hash":"XXXXXXXX"}
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var nodeId = root.TryGetProperty("nodeId", out var n) ? n.GetString() ?? "" : "";
            var firmware = root.TryGetProperty("firmware", out var f) ? f.GetString() ?? "" : "";
            var deviceName = root.TryGetProperty("deviceName", out var d) ? d.GetString() ?? "" : "";

            // Parse hash from hex string
            byte[] hash = new byte[4];
            if (root.TryGetProperty("hash", out var hashProp))
            {
                var hashStr = hashProp.GetString() ?? "";
                if (hashStr.Length >= 8)
                {
                    hash[0] = Convert.ToByte(hashStr.Substring(0, 2), 16);
                    hash[1] = Convert.ToByte(hashStr.Substring(2, 2), 16);
                    hash[2] = Convert.ToByte(hashStr.Substring(4, 2), 16);
                    hash[3] = Convert.ToByte(hashStr.Substring(6, 2), 16);
                }
            }

            // Use deviceName as hardware type (for now)
            return new BleDeviceInfoDto(nodeId, firmware, deviceName, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading device info");
            return null;
        }
    }

    public byte[] ComputeNodeIdHash(string nodeId)
    {
        // Compute hash same as ESP32: simple polynomial hash (hash = hash * 31 + char)
        // This matches the ESP32 implementation in ble_beacon_mode.cpp
        uint hash = 0;
        foreach (var c in nodeId)
        {
            hash = hash * 31 + c;
        }

        // Return as 4 bytes (big-endian, same as ESP32)
        return new byte[]
        {
            (byte)((hash >> 24) & 0xFF),
            (byte)((hash >> 16) & 0xFF),
            (byte)((hash >> 8) & 0xFF),
            (byte)(hash & 0xFF)
        };
    }

    public async Task<BleAuthResultDto> ConnectAndAuthenticateAsync(string macAddress, string nodeId, CancellationToken ct = default)
    {
        // Connect
        var connectResult = await ConnectAsync(macAddress, ct);
        if (!connectResult.Success)
        {
            return new BleAuthResultDto(false, macAddress, BleResponseCode.Error, connectResult.Message);
        }

        // Authenticate with computed hash
        var hash = ComputeNodeIdHash(nodeId);
        return await AuthenticateAsync(hash, ct);
    }

    public async Task<BleConfigResultDto> ProvisionDeviceAsync(
        string macAddress,
        string nodeId,
        BleWifiConfigDto wifiConfig,
        BleHubUrlConfigDto hubUrlConfig,
        uint? intervalSeconds = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Provisioning device {MacAddress} with Node ID {NodeId}", macAddress, nodeId);

            // Connect and authenticate
            var authResult = await ConnectAndAuthenticateAsync(macAddress, nodeId, ct);
            if (!authResult.Success)
            {
                return new BleConfigResultDto(false, BleConfigCommand.Authenticate, authResult.ResponseCode,
                    $"Authentication failed: {authResult.Message}");
            }

            // Set Node ID
            var nodeIdResult = await SetNodeIdAsync(nodeId, ct);
            if (!nodeIdResult.Success)
            {
                return nodeIdResult;
            }

            // Set WiFi
            var wifiResult = await SetWifiAsync(wifiConfig, ct);
            if (!wifiResult.Success)
            {
                return wifiResult;
            }

            // Set Hub URL
            var hubResult = await SetHubUrlAsync(hubUrlConfig, ct);
            if (!hubResult.Success)
            {
                return hubResult;
            }

            // Set interval if specified
            if (intervalSeconds.HasValue)
            {
                var intervalResult = await SetIntervalAsync(intervalSeconds.Value, ct);
                if (!intervalResult.Success)
                {
                    return intervalResult;
                }
            }

            _logger.LogInformation("Device {MacAddress} provisioned successfully", macAddress);

            // Reboot to apply settings
            return await RebootAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning device {MacAddress}", macAddress);
            return new BleConfigResultDto(false, BleConfigCommand.Authenticate, BleResponseCode.Error, ex.Message);
        }
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }
}
