namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for Node Hardware Status (Sprint 8: Hardware Status Reporting).
/// Reports detected hardware, sensor status, and storage information.
/// </summary>
public record NodeHardwareStatusDto(
    Guid NodeId,
    string SerialNumber,
    string FirmwareVersion,
    string HardwareType,
    DateTime ReportedAt,
    HardwareSummaryDto Summary,
    List<DetectedDeviceDto> DetectedDevices,
    StorageStatusDto Storage,
    BusStatusDto BusStatus
);

/// <summary>
/// Summary of hardware status.
/// </summary>
public record HardwareSummaryDto(
    int TotalDevicesDetected,
    int SensorsConfigured,
    int SensorsOk,
    int SensorsError,
    bool HasSdCard,
    bool HasGps,
    string OverallStatus // "OK", "Warning", "Error"
);

/// <summary>
/// DTO for a detected hardware device.
/// </summary>
public record DetectedDeviceDto(
    string DeviceType,        // e.g., "BME280", "DS18B20", "GPS"
    string Bus,               // "I2C", "OneWire", "UART", "Analog"
    string Address,           // e.g., "0x76", "28:FF:...", "GPIO34"
    string Status,            // "OK", "Error", "NotConfigured"
    string? SensorCode,       // Assigned sensor code if configured
    int? EndpointId,          // Assigned endpoint if configured
    string? ErrorMessage      // Error details if status is "Error"
);

/// <summary>
/// DTO for storage (SD card) status.
/// </summary>
public record StorageStatusDto(
    bool Available,
    string Mode,              // "REMOTE_ONLY", "LOCAL_AND_REMOTE", "LOCAL_ONLY", "LOCAL_AUTOSYNC"
    long TotalBytes,
    long UsedBytes,
    long FreeBytes,
    int PendingSyncCount,
    DateTime? LastSyncAt,
    string? LastSyncError
);

/// <summary>
/// DTO for bus status (I2C, UART, etc.).
/// </summary>
public record BusStatusDto(
    bool I2cAvailable,
    int I2cDeviceCount,
    List<string> I2cAddresses,
    bool OneWireAvailable,
    int OneWireDeviceCount,
    bool UartAvailable,
    bool GpsDetected
);

/// <summary>
/// DTO for reporting hardware status from firmware.
/// </summary>
public record ReportHardwareStatusDto(
    string SerialNumber,
    string FirmwareVersion,
    string HardwareType,
    List<DetectedDeviceDto> DetectedDevices,
    StorageStatusDto Storage,
    BusStatusDto BusStatus
);
