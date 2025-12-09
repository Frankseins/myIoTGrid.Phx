namespace myIoTGrid.Hub.Infrastructure.Matter;

/// <summary>
/// Client interface for communicating with the Matter Bridge
/// </summary>
public interface IMatterBridgeClient
{
    /// <summary>
    /// Check if the Matter Bridge is available and running
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the current status of the Matter Bridge
    /// </summary>
    Task<MatterBridgeStatus?> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Register a new device with the Matter Bridge
    /// </summary>
    Task<bool> RegisterDeviceAsync(
        string sensorId,
        string name,
        string type,
        string? location = null,
        CancellationToken ct = default);

    /// <summary>
    /// Update a device's sensor value
    /// </summary>
    Task<bool> UpdateDeviceValueAsync(
        string sensorId,
        string sensorType,
        double value,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a device from the Matter Bridge
    /// </summary>
    Task<bool> RemoveDeviceAsync(string sensorId, CancellationToken ct = default);

    /// <summary>
    /// Set the state of a contact sensor (for alerts)
    /// </summary>
    Task<bool> SetContactSensorStateAsync(
        string sensorId,
        bool isOpen,
        CancellationToken ct = default);

    /// <summary>
    /// Get commissioning information for pairing
    /// </summary>
    Task<MatterCommissionInfo?> GetCommissionInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Generate QR code for pairing
    /// </summary>
    Task<MatterQrCodeInfo?> GenerateQrCodeAsync(CancellationToken ct = default);
}

/// <summary>
/// Matter Bridge status information
/// </summary>
public record MatterBridgeStatus(
    bool IsStarted,
    int DeviceCount,
    IReadOnlyList<MatterDeviceInfo> Devices,
    int PairingCode,
    int Discriminator
);

/// <summary>
/// Information about a registered Matter device
/// </summary>
public record MatterDeviceInfo(
    string SensorId,
    string Name,
    string Type,
    string? Location
);

/// <summary>
/// Matter commissioning information
/// </summary>
public record MatterCommissionInfo(
    int PairingCode,
    int Discriminator,
    string ManualPairingCode,
    string QrCodeData
);

/// <summary>
/// QR code information for pairing
/// </summary>
public record MatterQrCodeInfo(
    string QrCodeData,
    string QrCodeImage, // Base64 encoded image
    string ManualPairingCode
);
