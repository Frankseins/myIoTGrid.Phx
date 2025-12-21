using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service for Bluetooth device pairing using bluetoothctl on Linux.
/// This is used when pairing BLE sensors directly on the Raspberry Pi Hub.
/// Sprint BT-02: Backend Bluetooth Pairing
/// </summary>
public interface IBluetoothPairingService
{
    /// <summary>
    /// Scans for BLE devices with myIoTGrid prefix
    /// </summary>
    Task<List<ScannedBleDeviceDto>> ScanForDevicesAsync(int timeoutSeconds = 10, CancellationToken ct = default);

    /// <summary>
    /// Pairs with a BLE device by MAC address
    /// </summary>
    Task<BlePairingResultDto> PairDeviceAsync(string macAddress, CancellationToken ct = default);

    /// <summary>
    /// Trusts a paired device (required for auto-reconnect)
    /// </summary>
    Task<bool> TrustDeviceAsync(string macAddress, CancellationToken ct = default);

    /// <summary>
    /// Removes pairing for a device
    /// </summary>
    Task<bool> UnpairDeviceAsync(string macAddress, CancellationToken ct = default);

    /// <summary>
    /// Checks if a device is paired
    /// </summary>
    Task<bool> IsDevicePairedAsync(string macAddress, CancellationToken ct = default);

    /// <summary>
    /// Gets list of paired devices
    /// </summary>
    Task<List<ScannedBleDeviceDto>> GetPairedDevicesAsync(CancellationToken ct = default);
}
