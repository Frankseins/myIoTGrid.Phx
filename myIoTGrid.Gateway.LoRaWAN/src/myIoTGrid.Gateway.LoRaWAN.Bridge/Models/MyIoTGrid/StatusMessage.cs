using System.Text.Json.Serialization;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Models.MyIoTGrid;

/// <summary>
/// myIoTGrid Gateway Status Message
/// Status des Gateway Bridge Services
/// </summary>
public class StatusMessage
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("connectedToChirpStack")]
    public bool ConnectedToChirpStack { get; set; }

    [JsonPropertyName("connectedToMyIoTGrid")]
    public bool ConnectedToMyIoTGrid { get; set; }

    [JsonPropertyName("statistics")]
    public BridgeStatistics? Statistics { get; set; }
}

public class BridgeStatistics
{
    [JsonPropertyName("uplinksReceived")]
    public long UplinksReceived { get; set; }

    [JsonPropertyName("readingsPublished")]
    public long ReadingsPublished { get; set; }

    [JsonPropertyName("joinsReceived")]
    public long JoinsReceived { get; set; }

    [JsonPropertyName("errors")]
    public long Errors { get; set; }

    [JsonPropertyName("lastUplinkAt")]
    public DateTime? LastUplinkAt { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }
}
