using InTheHand.Bluetooth;
using myIoTGrid.BluetoothHub.Models;

namespace myIoTGrid.BluetoothHub.Services;

public class BluetoothScannerService
{
    private readonly ILogger<BluetoothScannerService> _logger;
    private readonly HubConfiguration _config;

    public BluetoothScannerService(
        ILogger<BluetoothScannerService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _config = configuration.GetSection("BluetoothHub").Get<HubConfiguration>()
            ?? throw new InvalidOperationException("BluetoothHub configuration missing");
    }

    public async Task<List<BleDevice>> ScanForDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = new List<BleDevice>();

        try
        {
            _logger.LogInformation("Starting BLE scan...");

            // Check if Bluetooth is available
            if (!await Bluetooth.GetAvailabilityAsync())
            {
                _logger.LogWarning("Bluetooth is not available on this system");
                return devices;
            }

            _logger.LogDebug("Bluetooth is available, scanning for devices with prefix 'myIoTGrid-' or 'ESP32-'");

            // Note: InTheHand.BluetoothLE RequestDevice is for interactive scenarios (browser-like)
            // For a background service on Linux, we need BlueZ D-Bus API
            // This is a placeholder implementation that logs the limitation

            var requestOptions = new RequestDeviceOptions
            {
                AcceptAllDevices = false
            };
            requestOptions.Filters.Add(new BluetoothLEScanFilter { NamePrefix = "myIoTGrid-" });
            requestOptions.Filters.Add(new BluetoothLEScanFilter { NamePrefix = "ESP32-" });

            _logger.LogDebug("Configured scan filters: myIoTGrid-*, ESP32-*");
            _logger.LogWarning("Interactive RequestDevice not suitable for background service");
            _logger.LogInformation("Note: For production on Linux, implement BlueZ D-Bus scanning");

            // TODO: Implement background BLE scanning using BlueZ D-Bus API
            // This requires platform-specific implementation:
            // - Linux: BlueZ D-Bus (Tmds.DBus NuGet package)
            // - Windows: Windows.Devices.Bluetooth.Advertisement
            // - macOS: CoreBluetooth (limited background support)

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during BLE scan");
            return devices;
        }
    }

    public bool IsDeviceRegistered(string deviceName, string nodeId)
    {
        return _config.RegisteredDevices.Any(d =>
            d.Name == deviceName || d.NodeId == nodeId);
    }

    public RegisteredDevice? GetRegisteredDevice(string deviceName, string nodeId)
    {
        return _config.RegisteredDevices.FirstOrDefault(d =>
            d.Name == deviceName || d.NodeId == nodeId);
    }

    public IEnumerable<RegisteredDevice> GetAllRegisteredDevices()
    {
        return _config.RegisteredDevices;
    }
}
