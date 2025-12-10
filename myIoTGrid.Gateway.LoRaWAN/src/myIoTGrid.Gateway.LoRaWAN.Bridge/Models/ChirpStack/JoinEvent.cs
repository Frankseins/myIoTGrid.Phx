using System.Text.Json.Serialization;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Models.ChirpStack;

/// <summary>
/// ChirpStack Join Event
/// Empfangen wenn ein LoRaWAN Device dem Netzwerk beitritt (OTAA)
/// </summary>
public class JoinEvent
{
    [JsonPropertyName("deduplicationId")]
    public string? DeduplicationId { get; set; }

    [JsonPropertyName("time")]
    public DateTime? Time { get; set; }

    [JsonPropertyName("deviceInfo")]
    public DeviceInfo? DeviceInfo { get; set; }

    [JsonPropertyName("devAddr")]
    public string? DevAddr { get; set; }

    [JsonPropertyName("relayRxInfo")]
    public object? RelayRxInfo { get; set; }
}
