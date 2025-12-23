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

/// <summary>
/// DTO for scanned BLE devices (from backend bluetoothctl scan)
/// Sprint BT-02: Backend Bluetooth Pairing
/// </summary>
public record ScannedBleDeviceDto(
    string MacAddress,
    string Name
);

/// <summary>
/// DTO for BLE pairing result
/// Sprint BT-02: Backend Bluetooth Pairing
/// </summary>
public record BlePairingResultDto(
    bool Success,
    string MacAddress,
    string? DeviceName,
    string Message
);

/// <summary>
/// DTO for requesting backend BLE pairing
/// Sprint BT-02: Backend Bluetooth Pairing
/// </summary>
public record PairBleDeviceRequestDto(
    string MacAddress,
    string? NodeId = null
);

// ============================================================================
// BLE GATT Client DTOs (Sprint BT-01)
// For bidirectional communication with ESP32 sensors via GATT
// ============================================================================

/// <summary>
/// Response codes from ESP32 BLE GATT service
/// </summary>
public enum BleResponseCode : byte
{
    Ok = 0x00,
    Error = 0x01,
    InvalidCommand = 0x02,
    InvalidData = 0x03,
    NotAuthenticated = 0x04
}

/// <summary>
/// Config commands for ESP32 BLE GATT service
/// </summary>
public enum BleConfigCommand : byte
{
    Authenticate = 0x00,
    SetWifi = 0x01,
    SetHubUrl = 0x02,
    SetNodeId = 0x03,
    SetInterval = 0x04,
    FactoryReset = 0xFE,
    Reboot = 0xFF
}

/// <summary>
/// Device info read from ESP32 via BLE GATT
/// </summary>
public record BleDeviceInfoDto(
    string NodeId,
    string FirmwareVersion,
    string HardwareType,
    byte[] NodeIdHash
);

/// <summary>
/// Result of BLE GATT connection attempt
/// </summary>
public record BleConnectionResultDto(
    bool Success,
    string MacAddress,
    string? DeviceName,
    BleDeviceInfoDto? DeviceInfo,
    string Message
);

/// <summary>
/// Result of BLE GATT authentication
/// </summary>
public record BleAuthResultDto(
    bool Success,
    string MacAddress,
    BleResponseCode ResponseCode,
    string Message
);

/// <summary>
/// Result of sending a config command via BLE GATT
/// </summary>
public record BleConfigResultDto(
    bool Success,
    BleConfigCommand Command,
    BleResponseCode ResponseCode,
    string Message
);

/// <summary>
/// WiFi configuration to send to ESP32
/// </summary>
public record BleWifiConfigDto(
    string Ssid,
    string Password
);

/// <summary>
/// Hub URL configuration to send to ESP32
/// </summary>
public record BleHubUrlConfigDto(
    string HubUrl,
    int Port = 5001,
    string Protocol = "https"
);

/// <summary>
/// Sensor data read from ESP32 via BLE GATT
/// </summary>
public record BleSensorDataDto(
    float Temperature,
    float Humidity,
    float Pressure,
    ushort BatteryMv,
    DateTime Timestamp
);

/// <summary>
/// Request DTO for provisioning an ESP32 via BLE GATT
/// </summary>
public record BleProvisioningRequestDto(
    string MacAddress,
    string NodeId,
    BleWifiConfigDto WifiConfig,
    BleHubUrlConfigDto HubUrlConfig,
    uint? IntervalSeconds = null
);
