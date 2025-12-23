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
    /// Uses AdvertisementReceived event to get manufacturer data without connecting.
    /// </summary>
    private async Task ScanForBeaconsAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting beacon scan for myIoTGrid devices...");

        try
        {
            var foundDevices = new Dictionary<string, bool>();
            var scanComplete = new TaskCompletionSource<bool>();

            // Subscribe to advertisement received events
            void OnAdvertisementReceived(object? sender, BluetoothAdvertisingEvent e)
            {
                try
                {
                    if (!IsMyIoTGridDevice(e.Name)) return;
                    if (foundDevices.ContainsKey(e.Device.Id)) return;
                    foundDevices[e.Device.Id] = true;

                    _logger.LogInformation("Found myIoTGrid beacon: {Name} ({Id})", e.Name, e.Device.Id);

                    // Try to read manufacturer data from advertising packet
                    if (e.ManufacturerData != null && e.ManufacturerData.Count > 0)
                    {
                        foreach (var mfgData in e.ManufacturerData)
                        {
                            _logger.LogInformation("Manufacturer data from {Name}: CompanyId=0x{CompanyId:X4}, {Length} bytes",
                                e.Name, mfgData.Key, mfgData.Value.Length);

                            // Parse sensor data from manufacturer data
                            // Format: [temp:2][humidity:2][pressure:2][battery:2] = 8 bytes
                            if (mfgData.Value.Length >= 8)
                            {
                                var data = mfgData.Value;
                                var temp = BitConverter.ToInt16(data, 0) / 100.0;
                                var humidity = BitConverter.ToUInt16(data, 2) / 100.0;
                                var pressureRaw = BitConverter.ToUInt16(data, 4);
                                var pressure = (pressureRaw + 50000) / 100.0;
                                var battery = BitConverter.ToUInt16(data, 6);

                                _logger.LogInformation("Beacon sensor data from {Name}: T={Temp:F1}°C H={Humidity:F0}% P={Pressure:F0}hPa Bat={Battery}mV",
                                    e.Name, temp, humidity, pressure, battery);

                                // Process the sensor data
                                _ = ProcessBeaconManufacturerDataAsync(e.Name ?? e.Device.Id, temp, humidity, pressure);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogDebug("No manufacturer data in advertising from {Name}", e.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing advertisement from {Name}", e.Name);
                }
            }

            Bluetooth.AdvertisementReceived += OnAdvertisementReceived;

            try
            {
                // Start scanning
                var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(TimeSpan.FromSeconds(15));

                // Request scan - this triggers AdvertisementReceived events
                await Bluetooth.RequestLEScanAsync(new BluetoothLEScanOptions
                {
                    AcceptAllAdvertisements = true
                });

                // Wait for scan duration
                await Task.Delay(TimeSpan.FromSeconds(12), cts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // Scan timeout - normal
            }
            finally
            {
                Bluetooth.AdvertisementReceived -= OnAdvertisementReceived;
            }

            _logger.LogDebug("Beacon scan complete. Found {Count} myIoTGrid devices", foundDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during beacon scan");
        }
    }

    /// <summary>
    /// Processes sensor data from beacon manufacturer data
    /// </summary>
    private async Task ProcessBeaconManufacturerDataAsync(string deviceName, double temperature, double humidity, double pressure)
    {
        try
        {
            // Extract node ID from device name (e.g., "myIoTGrid-92CC" -> use as identifier)
            var nodeId = deviceName;

            using var scope = _scopeFactory.CreateScope();
            var readingService = scope.ServiceProvider.GetRequiredService<IReadingService>();

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Create readings for each sensor value (skip zeros)
            if (temperature != 0)
            {
                await readingService.CreateFromSensorAsync(new CreateSensorReadingDto(
                    DeviceId: nodeId,
                    Type: "temperature",
                    Value: temperature,
                    Unit: "°C",
                    Timestamp: timestamp
                ));
            }

            if (humidity != 0)
            {
                await readingService.CreateFromSensorAsync(new CreateSensorReadingDto(
                    DeviceId: nodeId,
                    Type: "humidity",
                    Value: humidity,
                    Unit: "%",
                    Timestamp: timestamp
                ));
            }

            if (pressure != 0 && pressure > 800 && pressure < 1200)  // Valid pressure range
            {
                await readingService.CreateFromSensorAsync(new CreateSensorReadingDto(
                    DeviceId: nodeId,
                    Type: "pressure",
                    Value: pressure,
                    Unit: "hPa",
                    Timestamp: timestamp
                ));
            }

            _logger.LogInformation("Processed beacon data from {NodeId}: T={Temp:F1}°C H={Humidity:F0}% P={Pressure:F0}hPa",
                nodeId, temperature, humidity, pressure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing beacon manufacturer data from {DeviceName}", deviceName);
        }
    }

    /// <summary>
    /// Reads sensor data from a beacon device via quick GATT connection.
    /// Includes retry logic for BlueZ connection timeouts.
    /// </summary>
    private async Task TryReadBeaconDataAsync(BluetoothDevice device, CancellationToken stoppingToken)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 1500;

        try
        {
            var gatt = device.Gatt;

            // Retry loop for GATT connection
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("GATT connection attempt {Attempt}/{Max} to {DeviceName}...",
                        attempt, maxRetries, device.Name);

                    await gatt.ConnectAsync();

                    if (gatt.IsConnected)
                    {
                        _logger.LogInformation("Connected to {DeviceName} on attempt {Attempt}", device.Name, attempt);
                        break;
                    }
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning("Connection attempt {Attempt} failed: {Message}. Retrying in {Delay}ms...",
                        attempt, ex.Message, retryDelayMs);
                    await Task.Delay(retryDelayMs, stoppingToken);
                }
            }

            if (!gatt.IsConnected)
            {
                _logger.LogWarning("Could not connect to {DeviceName} after {Retries} attempts", device.Name, maxRetries);
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
