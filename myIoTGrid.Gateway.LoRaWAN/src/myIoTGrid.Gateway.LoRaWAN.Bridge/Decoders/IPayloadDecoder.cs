namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Decoders;

/// <summary>
/// Interface für Payload Decoder
/// Dekodiert LoRaWAN Payloads zu Sensor-Readings
/// </summary>
public interface IPayloadDecoder
{
    /// <summary>
    /// Name des Decoders
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Dekodiert ein LoRaWAN Payload zu Sensor-Readings
    /// </summary>
    /// <param name="payload">Raw LoRaWAN Payload</param>
    /// <param name="devEui">Device EUI</param>
    /// <param name="fPort">LoRaWAN FPort</param>
    /// <returns>Liste der dekodierten Readings</returns>
    IEnumerable<DecodedReading> Decode(byte[] payload, string devEui, int fPort);

    /// <summary>
    /// Prüft ob dieser Decoder das Payload verarbeiten kann
    /// </summary>
    bool CanDecode(byte[] payload, int fPort);
}

/// <summary>
/// Dekodierter Sensor-Wert
/// </summary>
public class DecodedReading
{
    /// <summary>
    /// Device EUI
    /// </summary>
    public string DevEui { get; set; } = string.Empty;

    /// <summary>
    /// Sensor Type Code (0x01 = temperature, etc.)
    /// </summary>
    public byte TypeCode { get; set; }

    /// <summary>
    /// Sensor Typ Name (z.B. "temperature")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Dekodierter Wert
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Einheit (z.B. "°C")
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Zeitstempel der Dekodierung
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Zusätzliche Metadaten
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
