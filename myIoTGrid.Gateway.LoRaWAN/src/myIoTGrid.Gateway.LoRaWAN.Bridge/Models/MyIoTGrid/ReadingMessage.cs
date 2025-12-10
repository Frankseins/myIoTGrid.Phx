using System.Text.Json.Serialization;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Models.MyIoTGrid;

/// <summary>
/// myIoTGrid Reading Message
/// Format für Sensor-Daten die an myIoTGrid.Hub gesendet werden
/// </summary>
public class ReadingMessage
{
    [JsonPropertyName("nodeId")]
    public Guid NodeId { get; set; }

    [JsonPropertyName("sensorId")]
    public Guid SensorId { get; set; }

    [JsonPropertyName("sensorType")]
    public string SensorType { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
