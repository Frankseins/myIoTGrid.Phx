namespace myIoTGrid.Shared.Common.DTOs.Discovery;

/// <summary>
/// DTO for UDP discovery response sent by hub to sensors.
/// Contains all information a sensor needs to connect to the hub.
/// </summary>
/// <param name="MessageType">Must be "MYIOTGRID_HUB" to identify the response</param>
/// <param name="HubId">Unique hub identifier</param>
/// <param name="HubName">Display name of the hub</param>
/// <param name="ApiUrl">Full URL to the hub API (e.g., "https://192.168.1.100:5001")</param>
/// <param name="ApiVersion">API version for compatibility checking</param>
/// <param name="ProtocolVersion">Discovery protocol version</param>
public record DiscoveryResponseDto(
    string MessageType,
    string HubId,
    string HubName,
    string ApiUrl,
    string ApiVersion,
    string ProtocolVersion
)
{
    /// <summary>
    /// Expected message type for discovery responses
    /// </summary>
    public const string ExpectedMessageType = "MYIOTGRID_HUB";

    /// <summary>
    /// Current API version
    /// </summary>
    public const string CurrentApiVersion = "1.0";

    /// <summary>
    /// Current protocol version for discovery
    /// </summary>
    public const string CurrentProtocolVersion = "1.0";

    /// <summary>
    /// Creates a new discovery response with the expected message type
    /// </summary>
    public static DiscoveryResponseDto Create(string hubId, string hubName, string apiUrl)
        => new(ExpectedMessageType, hubId, hubName, apiUrl, CurrentApiVersion, CurrentProtocolVersion);

    /// <summary>
    /// Validates that the message type is correct
    /// </summary>
    public bool IsValid => MessageType == ExpectedMessageType;
}
