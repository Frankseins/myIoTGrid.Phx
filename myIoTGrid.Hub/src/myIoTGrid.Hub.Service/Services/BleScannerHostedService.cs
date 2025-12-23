using System.Text;
using System.Text.Json;
using InTheHand.Bluetooth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Background service that scans for BLE devices (ESP32 sensors) and receives sensor data.
/// This runs directly in the Hub, eliminating the need for a separate BluetoothHub service.
/// Sprint BT-01: Bluetooth Infrastructure
/// </summary>
public class BleScannerHostedService : BackgroundService
{
    private readonly ILogger<BleScannerHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    // BLE UUIDs (must match ESP32 firmware)
    private readonly Guid _serviceUuid;
    private readonly Guid _sensorDataUuid;
    private readonly Guid _deviceInfoUuid;

    // Connected devices
    private readonly Dictionary<string, BluetoothDevice> _connectedDevices = new();
    private readonly Dictionary<string, GattCharacteristic> _sensorDataCharacteristics = new();

    // Configuration
    private readonly int _scanIntervalMs;
    private readonly bool _enabled;

    public BleScannerHostedService(
        ILogger<BleScannerHostedService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;

        // Load BLE configuration
        var bleConfig = _configuration.GetSection("Ble");
        _enabled = bleConfig.GetValue("Enabled", false);
        _scanIntervalMs = bleConfig.GetValue("ScanIntervalMs", 30000);

        // UUIDs from config or defaults (must match ESP32 firmware config.h ble_sensor namespace!)
        // ESP32 BLE Sensor Mode UUIDs (Sprint BT-01)
        var serviceUuidStr = bleConfig.GetValue("ServiceUuid", "12345678-1234-5678-1234-56789abcdef0");
        var sensorDataUuidStr = bleConfig.GetValue("SensorDataUuid", "12345678-1234-5678-1234-56789abcdef1");
        var deviceInfoUuidStr = bleConfig.GetValue("DeviceInfoUuid", "12345678-1234-5678-1234-56789abcdef2");

        _serviceUuid = Guid.Parse(serviceUuidStr!);
        _sensorDataUuid = Guid.Parse(sensorDataUuidStr!);
        _deviceInfoUuid = Guid.Parse(deviceInfoUuidStr!);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("BLE Scanner is disabled in configuration. Set Ble:Enabled=true to enable");
            return;
        }

        _logger.LogInformation("BLE Scanner starting...");
        _logger.LogInformation("Scan interval: {Interval}ms", _scanIntervalMs);

        // Wait for application to fully start
        await Task.Delay(5000, stoppingToken);

        // Check Bluetooth availability
        var isAvailable = await CheckBluetoothAvailabilityAsync();
        if (!isAvailable)
        {
            _logger.LogError("Bluetooth is not available on this system. BLE Scanner will not run.");
            return;
        }

        _logger.LogInformation("Bluetooth is available. Starting BEACON scan loop...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Scan for BLE Beacons (Advertising data - no connection needed!)
                await ScanForBeaconsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BLE beacon scan loop");
            }

            await Task.Delay(_scanIntervalMs, stoppingToken);
        }

        _logger.LogInformation("BLE Scanner stopping...");
        DisconnectAllDevices();
    }

    /// <summary>
    /// Scans for BLE Beacons and reads sensor data from advertising packets.
    /// No connection required - just reads manufacturer data from advertising.
    /// </summary>
    private async Task ScanForBeaconsAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting beacon scan for myIoTGrid devices...");

        try
        {
            // Scan for 10 seconds
            var scanDuration = TimeSpan.FromSeconds(10);
            var foundDevices = new HashSet<string>();

            var scannedDevices = await Bluetooth.ScanForDevicesAsync();

            foreach (var device in scannedDevices)
            {
                if (stoppingToken.IsCancellationRequested) break;

                // Check if this is a myIoTGrid device
                if (!IsMyIoTGridDevice(device.Name)) continue;

                if (foundDevices.Contains(device.Id)) continue;
                foundDevices.Add(device.Id);

                _logger.LogInformation("Found myIoTGrid beacon: {Name} ({Id})", device.Name, device.Id);

                // Try to read advertising data via GATT connection
                // (InTheHand.BLE doesn't expose raw advertising data directly)
                await TryReadBeaconDataAsync(device, stoppingToken);
            }

            _logger.LogDebug("Beacon scan complete. Found {Count} myIoTGrid devices", foundDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during beacon scan");
        }
    }

    /// <summary>
    /// Reads sensor data from a beacon device via quick GATT connection.
    /// </summary>
    private async Task TryReadBeaconDataAsync(BluetoothDevice device, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Attempting GATT connection to {DeviceName} ({DeviceId})...", device.Name, device.Id);

            // Connect briefly to read sensor data characteristic
            var gatt = device.Gatt;
            await gatt.ConnectAsync();

            if (!gatt.IsConnected)
            {
                _logger.LogWarning("Could not connect to {DeviceName} for data read", device.Name);
                return;
            }

            _logger.LogInformation("Connected to {DeviceName}, discovering services...", device.Name);

            // Try to get the Config Service (Beacon Mode uses different UUIDs!)
            // CONFIG_SERVICE_UUID = "4d494f54-4752-4944-434f-4e4649470000"
            var beaconServiceUuid = Guid.Parse("4d494f54-4752-4944-434f-4e4649470000");
            var sensorDataCharUuid = Guid.Parse("4d494f54-4752-4944-434f-4e4649470003");
            var configReadCharUuid = Guid.Parse("4d494f54-4752-4944-434f-4e4649470002");

            var service = await gatt.GetPrimaryServiceAsync(BluetoothUuid.FromGuid(beaconServiceUuid));
            if (service == null)
            {
                _logger.LogDebug("Beacon service not found on {DeviceName}, trying legacy service", device.Name);
                // Try legacy service UUID
                service = await gatt.GetPrimaryServiceAsync(BluetoothUuid.FromGuid(_serviceUuid));
            }

            if (service == null)
            {
                _logger.LogWarning("No compatible BLE service found on {DeviceName}", device.Name);
                gatt.Disconnect();
                return;
            }

            // Read device info to get Node ID
            string nodeId = device.Name ?? "Unknown";
            var configReadChar = await service.GetCharacteristicAsync(BluetoothUuid.FromGuid(configReadCharUuid));
            if (configReadChar != null)
            {
                var deviceInfoData = await configReadChar.ReadValueAsync();
                if (deviceInfoData != null && deviceInfoData.Length > 0)
                {
                    var deviceInfoJson = Encoding.UTF8.GetString(deviceInfoData);
                    _logger.LogDebug("Device info: {Json}", deviceInfoJson);

                    try
                    {
                        var deviceInfo = JsonSerializer.Deserialize<BeaconDeviceInfo>(deviceInfoJson);
                        if (deviceInfo?.NodeId != null)
                        {
                            nodeId = deviceInfo.NodeId;
                        }
                    }
                    catch { /* Ignore parse errors */ }
                }
            }

            // Read sensor data
            var sensorDataChar = await service.GetCharacteristicAsync(BluetoothUuid.FromGuid(sensorDataCharUuid));
            if (sensorDataChar == null)
            {
                // Try legacy UUID
                sensorDataChar = await service.GetCharacteristicAsync(BluetoothUuid.FromGuid(_sensorDataUuid));
            }

            if (sensorDataChar != null)
            {
                var sensorData = await sensorDataChar.ReadValueAsync();
                if (sensorData != null && sensorData.Length > 0)
                {
                    var sensorJson = Encoding.UTF8.GetString(sensorData);
                    _logger.LogInformation("Sensor data from {NodeId}: {Json}", nodeId, sensorJson);

                    // Parse JSON format: {"t":21.50,"h":65.0,"p":1013.0,"b":3300,"f":0}
                    await ProcessBeaconSensorDataAsync(nodeId, sensorJson);
                }
            }

            gatt.Disconnect();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading beacon data from {DeviceName}: {Message}", device.Name, ex.Message);
        }
    }

    /// <summary>
    /// Processes sensor data from beacon JSON format
    /// </summary>
    private async Task ProcessBeaconSensorDataAsync(string nodeId, string jsonData)
    {
        try
        {
            var data = JsonSerializer.Deserialize<BeaconSensorData>(jsonData);
            if (data == null) return;

            using var scope = _scopeFactory.CreateScope();
            var readingService = scope.ServiceProvider.GetRequiredService<IReadingService>();

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Create readings for each sensor value
            if (data.Temperature.HasValue && data.Temperature != 0)
            {
                await readingService.CreateFromSensorAsync(new CreateSensorReadingDto(
                    DeviceId: nodeId,
                    Type: "temperature",
                    Value: data.Temperature.Value,
                    Unit: "°C",
                    Timestamp: timestamp
                ));
            }

            if (data.Humidity.HasValue && data.Humidity != 0)
            {
                await readingService.CreateFromSensorAsync(new CreateSensorReadingDto(
                    DeviceId: nodeId,
                    Type: "humidity",
                    Value: data.Humidity.Value,
                    Unit: "%",
                    Timestamp: timestamp
                ));
            }

            if (data.Pressure.HasValue && data.Pressure != 0)
            {
                await readingService.CreateFromSensorAsync(new CreateSensorReadingDto(
                    DeviceId: nodeId,
                    Type: "pressure",
                    Value: data.Pressure.Value,
                    Unit: "hPa",
                    Timestamp: timestamp
                ));
            }

            _logger.LogInformation("Processed beacon data from {NodeId}: T={Temp}°C H={Hum}% P={Press}hPa",
                nodeId, data.Temperature, data.Humidity, data.Pressure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing beacon sensor data");
        }
    }

    private async Task<bool> CheckBluetoothAvailabilityAsync()
    {
        try
        {
            var available = await Bluetooth.GetAvailabilityAsync();
            _logger.LogInformation("Bluetooth availability: {Available}", available);
            return available;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Bluetooth availability");
            return false;
        }
    }

    private async Task ScanAndConnectAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting BLE scan for myIoTGrid devices...");

        try
        {
            // Note: RequestDeviceAsync is interactive on some platforms
            // For background service on Linux, we need platform-specific handling
            // This is a simplified implementation that may need BlueZ D-Bus on Linux

            _logger.LogDebug("Scanning for devices with prefix 'myIoTGrid-' or 'ESP32-'...");

            // Try to scan using the paired devices API
            await ScanUsingPairedDevicesAsync(stoppingToken);
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogWarning("BLE scanning not supported on this platform: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BLE scan");
        }
    }

    private async Task ScanUsingPairedDevicesAsync(CancellationToken stoppingToken)
    {
        var foundDevices = new List<BluetoothDevice>();

        try
        {
            // Get registered BLE devices from database
            var registeredDevices = await GetRegisteredBleDevicesAsync();
            _logger.LogDebug("Found {Count} registered BLE devices in database", registeredDevices.Count);

            // Get paired devices from OS
            var pairedDevices = await Bluetooth.GetPairedDevicesAsync();

            foreach (var device in pairedDevices)
            {
                // Check if this is a myIoTGrid device by name prefix
                if (IsMyIoTGridDevice(device.Name))
                {
                    _logger.LogInformation("Found paired device: {Name} ({Id})", device.Name, device.Id);
                    foundDevices.Add(device);
                }
                // Also check if it's a registered device by BLE device name
                else if (registeredDevices.Any(r =>
                    r.BleDeviceName != null &&
                    device.Name != null &&
                    device.Name.Contains(r.BleDeviceName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Found registered device: {Name} ({Id})", device.Name, device.Id);
                    foundDevices.Add(device);
                }
            }

            // Try to connect to found devices
            foreach (var device in foundDevices)
            {
                if (!_connectedDevices.ContainsKey(device.Id))
                {
                    await TryConnectToDeviceAsync(device, stoppingToken);
                }
            }

            _logger.LogDebug("Device scan complete. Found {Count} myIoTGrid devices ({Registered} registered, {Connected} connected)",
                foundDevices.Count, registeredDevices.Count, _connectedDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in device scanning");
        }
    }

    /// <summary>
    /// Gets BLE devices registered via frontend (Web Bluetooth) from the database
    /// </summary>
    private async Task<List<RegisteredBleDevice>> GetRegisteredBleDevicesAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HubDbContext>();

            var devices = await context.Nodes
                .AsNoTracking()
                .Where(n => n.Protocol == Protocol.Bluetooth && n.BleDeviceName != null)
                .Select(n => new RegisteredBleDevice
                {
                    NodeId = n.NodeId,
                    BleDeviceName = n.BleDeviceName,
                    BleMacAddress = n.BleMacAddress
                })
                .ToListAsync();

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registered BLE devices");
            return new List<RegisteredBleDevice>();
        }
    }

    private bool IsMyIoTGridDevice(string? deviceName)
    {
        if (string.IsNullOrEmpty(deviceName)) return false;
        return deviceName.StartsWith("myIoTGrid-", StringComparison.OrdinalIgnoreCase) ||
               deviceName.StartsWith("ESP32-", StringComparison.OrdinalIgnoreCase);
    }

    private async Task TryConnectToDeviceAsync(BluetoothDevice device, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Attempting to connect to {DeviceName} ({DeviceId})", device.Name, device.Id);

            // Connect to GATT server
            var gatt = device.Gatt;
            await gatt.ConnectAsync();

            if (!gatt.IsConnected)
            {
                _logger.LogWarning("Failed to connect to {DeviceName}", device.Name);
                return;
            }

            _logger.LogInformation("Connected to {DeviceName}", device.Name);

            // Get our service
            var service = await gatt.GetPrimaryServiceAsync(BluetoothUuid.FromGuid(_serviceUuid));
            if (service == null)
            {
                _logger.LogWarning("Service {ServiceUuid} not found on {DeviceName}", _serviceUuid, device.Name);
                gatt.Disconnect();
                return;
            }

            // Get sensor data characteristic
            var sensorDataChar = await service.GetCharacteristicAsync(BluetoothUuid.FromGuid(_sensorDataUuid));
            if (sensorDataChar == null)
            {
                _logger.LogWarning("Sensor data characteristic not found on {DeviceName}", device.Name);
                gatt.Disconnect();
                return;
            }

            // Subscribe to notifications
            sensorDataChar.CharacteristicValueChanged += OnSensorDataReceived;
            await sensorDataChar.StartNotificationsAsync();

            // Store connection
            _connectedDevices[device.Id] = device;
            _sensorDataCharacteristics[device.Id] = sensorDataChar;

            _logger.LogInformation("Successfully connected and subscribed to {DeviceName}", device.Name);

            // Update BluetoothHub status in database
            await UpdateBluetoothHubStatusAsync(device, "Active");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to device {DeviceName}", device.Name);
        }
    }

    private async void OnSensorDataReceived(object? sender, GattCharacteristicValueChangedEventArgs e)
    {
        try
        {
            if (e.Value == null || e.Value.Length == 0)
            {
                _logger.LogWarning("Received empty BLE data");
                return;
            }

            var jsonData = Encoding.UTF8.GetString(e.Value);
            _logger.LogDebug("Received BLE data: {Data}", jsonData);

            // Parse the sensor data
            var sensorData = JsonSerializer.Deserialize<BleSensorDataPayload>(jsonData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (sensorData == null)
            {
                _logger.LogWarning("Failed to parse sensor data");
                return;
            }

            // Process the sensor data using scoped services
            await ProcessSensorDataAsync(sensorData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BLE sensor data");
        }
    }

    private async Task ProcessSensorDataAsync(BleSensorDataPayload payload)
    {
        using var scope = _scopeFactory.CreateScope();

        try
        {
            var readingService = scope.ServiceProvider.GetRequiredService<IReadingService>();

            // Create readings for each sensor value using CreateSensorReadingDto
            // This auto-creates the node if it doesn't exist
            if (payload.Sensors != null && payload.Sensors.Count > 0)
            {
                foreach (var sensor in payload.Sensors)
                {
                    var createDto = new CreateSensorReadingDto(
                        DeviceId: payload.NodeId,
                        Type: sensor.Type,
                        Value: sensor.Value,
                        Unit: sensor.Unit ?? GetDefaultUnit(sensor.Type),
                        Timestamp: payload.Timestamp
                    );

                    await readingService.CreateFromSensorAsync(createDto);
                }

                _logger.LogInformation("Processed {Count} sensor readings from BLE device {NodeId}",
                    payload.Sensors.Count, payload.NodeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sensor data from {NodeId}", payload.NodeId);
        }
    }

    private static string GetDefaultUnit(string measurementType)
    {
        return measurementType.ToLowerInvariant() switch
        {
            "temperature" => "°C",
            "humidity" => "%",
            "pressure" => "hPa",
            "co2" => "ppm",
            "light" or "lux" => "lux",
            "uv" => "index",
            "battery" => "%",
            "rssi" => "dBm",
            _ => ""
        };
    }

    private async Task UpdateBluetoothHubStatusAsync(BluetoothDevice device, string status)
    {
        using var scope = _scopeFactory.CreateScope();

        try
        {
            var bluetoothHubService = scope.ServiceProvider.GetRequiredService<IBluetoothHubService>();

            // Try to find by MAC address or create a default hub
            var btHub = await bluetoothHubService.GetByMacAddressAsync(device.Id);

            if (btHub != null)
            {
                await bluetoothHubService.SetStatusAsync(btHub.Id, status);
                await bluetoothHubService.UpdateLastSeenAsync(btHub.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating BluetoothHub status");
        }
    }

    private void DisconnectAllDevices()
    {
        foreach (var kvp in _connectedDevices)
        {
            try
            {
                if (_sensorDataCharacteristics.TryGetValue(kvp.Key, out var characteristic))
                {
                    characteristic.CharacteristicValueChanged -= OnSensorDataReceived;
                    // StopNotificationsAsync can throw, wrap it
                    try
                    {
                        characteristic.StopNotificationsAsync().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // Ignore errors during shutdown
                    }
                }

                kvp.Value.Gatt.Disconnect();
                _logger.LogInformation("Disconnected from {DeviceName}", kvp.Value.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from {DeviceId}", kvp.Key);
            }
        }

        _connectedDevices.Clear();
        _sensorDataCharacteristics.Clear();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BLE Scanner service stopping...");
        DisconnectAllDevices();
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Payload structure for BLE sensor data (must match ESP32 firmware)
/// </summary>
public class BleSensorDataPayload
{
    public string NodeId { get; set; } = string.Empty;
    public long? Timestamp { get; set; }
    public List<BleSensorReading> Sensors { get; set; } = new();
    public BleSensorGps? Gps { get; set; }
}

public class BleSensorReading
{
    public string Type { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Unit { get; set; }
}

public class BleSensorGps
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public float? Speed { get; set; }
    public int? Satellites { get; set; }
}

/// <summary>
/// Internal class to hold registered BLE device info from database
/// </summary>
internal class RegisteredBleDevice
{
    public string NodeId { get; set; } = string.Empty;
    public string? BleDeviceName { get; set; }
    public string? BleMacAddress { get; set; }
}

/// <summary>
/// Beacon device info (from CONFIG_READ characteristic)
/// JSON format: {"nodeId":"...","deviceName":"...","firmware":"...","hash":"..."}
/// </summary>
internal class BeaconDeviceInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("nodeId")]
    public string? NodeId { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("deviceName")]
    public string? DeviceName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("firmware")]
    public string? Firmware { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("hash")]
    public string? Hash { get; set; }
}

/// <summary>
/// Beacon sensor data (from SENSOR_DATA characteristic)
/// JSON format: {"t":21.50,"h":65.0,"p":1013.0,"b":3300,"f":0}
/// </summary>
internal class BeaconSensorData
{
    [System.Text.Json.Serialization.JsonPropertyName("t")]
    public double? Temperature { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("h")]
    public double? Humidity { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("p")]
    public double? Pressure { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("b")]
    public int? Battery { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("f")]
    public int? Flags { get; set; }
}
