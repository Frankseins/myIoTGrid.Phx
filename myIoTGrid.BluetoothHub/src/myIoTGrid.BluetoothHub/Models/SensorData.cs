using System.Text.Json.Serialization;

namespace myIoTGrid.BluetoothHub.Models;

public class SensorData
{
    [JsonPropertyName("nodeId")]
    public required string NodeId { get; set; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; set; }

    [JsonPropertyName("sensors")]
    public required SensorReadings Sensors { get; set; }

    [JsonPropertyName("battery")]
    public int? Battery { get; set; }

    [JsonPropertyName("rssi")]
    public int? Rssi { get; set; }
}

public class SensorReadings
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    [JsonPropertyName("pressure")]
    public double? Pressure { get; set; }

    [JsonPropertyName("uv")]
    public double? Uv { get; set; }

    [JsonPropertyName("waterLevel")]
    public double? WaterLevel { get; set; }

    [JsonPropertyName("gps")]
    public GpsData? Gps { get; set; }
}

public class GpsData
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("altitude")]
    public double? Altitude { get; set; }
}
