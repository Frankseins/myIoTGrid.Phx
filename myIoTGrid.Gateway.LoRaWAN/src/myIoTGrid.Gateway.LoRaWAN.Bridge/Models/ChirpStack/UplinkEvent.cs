using System.Text.Json.Serialization;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Models.ChirpStack;

/// <summary>
/// ChirpStack Uplink Event
/// Empfangen wenn ein LoRaWAN Device Daten sendet
/// </summary>
public class UplinkEvent
{
    [JsonPropertyName("deduplicationId")]
    public string? DeduplicationId { get; set; }

    [JsonPropertyName("time")]
    public DateTime? Time { get; set; }

    [JsonPropertyName("deviceInfo")]
    public DeviceInfo? DeviceInfo { get; set; }

    [JsonPropertyName("devAddr")]
    public string? DevAddr { get; set; }

    [JsonPropertyName("adr")]
    public bool Adr { get; set; }

    [JsonPropertyName("dr")]
    public int Dr { get; set; }

    [JsonPropertyName("fCnt")]
    public uint FCnt { get; set; }

    [JsonPropertyName("fPort")]
    public int FPort { get; set; }

    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("rxInfo")]
    public List<RxInfo>? RxInfo { get; set; }

    [JsonPropertyName("txInfo")]
    public TxInfo? TxInfo { get; set; }

    /// <summary>
    /// Dekodiert Base64 Data zu byte[]
    /// </summary>
    public byte[]? GetDecodedData()
    {
        if (string.IsNullOrEmpty(Data))
            return null;

        try
        {
            return Convert.FromBase64String(Data);
        }
        catch
        {
            return null;
        }
    }
}

public class DeviceInfo
{
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    [JsonPropertyName("tenantName")]
    public string? TenantName { get; set; }

    [JsonPropertyName("applicationId")]
    public string? ApplicationId { get; set; }

    [JsonPropertyName("applicationName")]
    public string? ApplicationName { get; set; }

    [JsonPropertyName("deviceProfileId")]
    public string? DeviceProfileId { get; set; }

    [JsonPropertyName("deviceProfileName")]
    public string? DeviceProfileName { get; set; }

    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("devEui")]
    public string? DevEui { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }
}

public class RxInfo
{
    [JsonPropertyName("gatewayId")]
    public string? GatewayId { get; set; }

    [JsonPropertyName("uplinkId")]
    public uint UplinkId { get; set; }

    [JsonPropertyName("nsTime")]
    public DateTime? NsTime { get; set; }

    [JsonPropertyName("rssi")]
    public int Rssi { get; set; }

    [JsonPropertyName("snr")]
    public double Snr { get; set; }

    [JsonPropertyName("channel")]
    public int Channel { get; set; }

    [JsonPropertyName("location")]
    public GatewayLocation? Location { get; set; }

    [JsonPropertyName("context")]
    public string? Context { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [JsonPropertyName("crcStatus")]
    public string? CrcStatus { get; set; }
}

public class GatewayLocation
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("altitude")]
    public double Altitude { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("accuracy")]
    public double Accuracy { get; set; }
}

public class TxInfo
{
    [JsonPropertyName("frequency")]
    public uint Frequency { get; set; }

    [JsonPropertyName("modulation")]
    public Modulation? Modulation { get; set; }
}

public class Modulation
{
    [JsonPropertyName("lora")]
    public LoRaModulation? LoRa { get; set; }
}

public class LoRaModulation
{
    [JsonPropertyName("bandwidth")]
    public uint Bandwidth { get; set; }

    [JsonPropertyName("spreadingFactor")]
    public int SpreadingFactor { get; set; }

    [JsonPropertyName("codeRate")]
    public string? CodeRate { get; set; }
}
