namespace myIoTGrid.Shared.Common.DTOs.Discovery;

/// <summary>
/// DTO for UDP discovery request sent by sensors to find hubs on the network.
/// Sensors broadcast this message to discover available hubs.
/// </summary>
/// <param name="MessageType">Must be "MYIOTGRID_DISCOVER" to identify the message</param>
/// <param name="Serial">Sensor serial number for identification</param>
/// <param name="FirmwareVersion">Current firmware version of the sensor</param>
/// <param name="HardwareType">Hardware type (e.g., "ESP32", "SIM")</param>
public record DiscoveryRequestDto(
    string MessageType,
    string Serial,
    string FirmwareVersion,
    string HardwareType
)
{
    /// <summary>
    /// Expected message type for discovery requests
    /// </summary>
    public const string ExpectedMessageType = "MYIOTGRID_DISCOVER";

    /// <summary>
    /// Creates a new discovery request with the expected message type
    /// </summary>
    public static DiscoveryRequestDto Create(string serial, string firmwareVersion, string hardwareType)
        => new(ExpectedMessageType, serial, firmwareVersion, hardwareType);

    /// <summary>
    /// Validates that the message type is correct
    /// </summary>
    public bool IsValid => MessageType == ExpectedMessageType;
}
