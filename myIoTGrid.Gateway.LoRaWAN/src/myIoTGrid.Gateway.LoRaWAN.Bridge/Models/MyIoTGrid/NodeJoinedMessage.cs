using System.Text.Json.Serialization;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Models.MyIoTGrid;

/// <summary>
/// myIoTGrid Node Joined Message
/// Gesendet wenn ein neues LoRaWAN Device dem Netzwerk beitritt
/// </summary>
public class NodeJoinedMessage
{
    [JsonPropertyName("nodeId")]
    public Guid NodeId { get; set; }

    [JsonPropertyName("devEui")]
    public string DevEui { get; set; } = string.Empty;

    [JsonPropertyName("devAddr")]
    public string? DevAddr { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "LoRaWAN";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
