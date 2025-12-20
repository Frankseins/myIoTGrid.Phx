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
        _logger.LogInformation("Service UUID: {ServiceUuid}", _serviceUuid);
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

        _logger.LogInformation("Bluetooth is available. Starting scan loop...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanAndConnectAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BLE scan loop");
            }

            await Task.Delay(_scanIntervalMs, stoppingToken);
        }

        _logger.LogInformation("BLE Scanner stopping...");
        DisconnectAllDevices();
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
