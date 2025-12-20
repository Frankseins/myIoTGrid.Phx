namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for BluetoothHub information.
/// Represents a Bluetooth gateway that receives sensor data via BLE.
/// </summary>
public record BluetoothHubDto(
    Guid Id,
    Guid HubId,
    string Name,
    string? MacAddress,
    string Status,
    DateTime? LastSeen,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int NodeCount
);

/// <summary>
/// DTO for creating a BluetoothHub
/// </summary>
public record CreateBluetoothHubDto(
    string Name,
    string? MacAddress = null,
    Guid? HubId = null
);

/// <summary>
/// DTO for updating a BluetoothHub
/// </summary>
public record UpdateBluetoothHubDto(
    string? Name = null,
    string? MacAddress = null,
    string? Status = null
);

/// <summary>
/// DTO for registering a BLE device paired via frontend (Web Bluetooth)
/// This allows the Hub to know about devices that should receive data via BLE.
/// </summary>
public record RegisterBleDeviceDto(
    string NodeId,
    string DeviceName,
    string? MacAddress = null,
    string? BluetoothDeviceId = null
);

/// <summary>
/// Response DTO for BLE device registration
/// </summary>
public record BleDeviceRegistrationResultDto(
    bool Success,
    Guid? NodeId,
    Guid? BluetoothHubId,
    string Message
);
