using myIoTGrid.Shared.Common.DTOs;

namespace myIoTGrid.Shared.Contracts.Services;

/// <summary>
/// Service Interface for BluetoothHub (Bluetooth Gateway) management.
/// BluetoothHubs are Raspberry Pi devices that receive sensor data via BLE
/// from ESP32 devices and forward it to the main Hub API.
/// </summary>
public interface IBluetoothHubService
{
    /// <summary>Returns all BluetoothHubs for the current Hub</summary>
    Task<IEnumerable<BluetoothHubDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a BluetoothHub by ID</summary>
    Task<BluetoothHubDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns a BluetoothHub by MAC address</summary>
    Task<BluetoothHubDto?> GetByMacAddressAsync(string macAddress, CancellationToken ct = default);

    /// <summary>Creates a new BluetoothHub</summary>
    Task<BluetoothHubDto> CreateAsync(CreateBluetoothHubDto dto, CancellationToken ct = default);

    /// <summary>Updates an existing BluetoothHub</summary>
    Task<BluetoothHubDto?> UpdateAsync(Guid id, UpdateBluetoothHubDto dto, CancellationToken ct = default);

    /// <summary>Deletes a BluetoothHub</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Updates the LastSeen timestamp (heartbeat)</summary>
    Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default);

    /// <summary>Sets the status (Active, Inactive, Error)</summary>
    Task SetStatusAsync(Guid id, string status, CancellationToken ct = default);

    /// <summary>Returns all Nodes connected via a specific BluetoothHub</summary>
    Task<IEnumerable<NodeDto>> GetNodesAsync(Guid bluetoothHubId, CancellationToken ct = default);

    /// <summary>Associates a Node with a BluetoothHub</summary>
    Task<bool> AssociateNodeAsync(Guid bluetoothHubId, Guid nodeId, CancellationToken ct = default);

    /// <summary>Removes the BluetoothHub association from a Node</summary>
    Task<bool> DisassociateNodeAsync(Guid nodeId, CancellationToken ct = default);

    /// <summary>
    /// Registers a BLE device that was paired via frontend (Web Bluetooth).
    /// Creates or gets a BluetoothHub for this Hub, creates the Node if needed,
    /// and associates them.
    /// </summary>
    Task<BleDeviceRegistrationResultDto> RegisterBleDeviceFromFrontendAsync(
        RegisterBleDeviceDto dto, CancellationToken ct = default);

    /// <summary>
    /// Gets or creates the default BluetoothHub for this Hub instance.
    /// Used when the Hub itself acts as the BLE gateway.
    /// </summary>
    Task<BluetoothHubDto> GetOrCreateDefaultAsync(CancellationToken ct = default);
}
