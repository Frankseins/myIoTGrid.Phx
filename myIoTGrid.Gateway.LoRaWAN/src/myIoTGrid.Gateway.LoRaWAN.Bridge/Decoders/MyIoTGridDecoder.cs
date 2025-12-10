namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Decoders;

/// <summary>
/// myIoTGrid Custom Payload Decoder
/// Format: [Type:1][Value:2 signed] pro Sensor (3 Bytes)
/// Unterstützt Multi-Sensor Payloads
/// </summary>
public class MyIoTGridDecoder : IPayloadDecoder
{
    private readonly ILogger<MyIoTGridDecoder> _logger;

    public string Name => "myIoTGrid";

    /// <summary>
    /// Sensor Type Mapping
    /// Key: Type Code (1 Byte)
    /// Value: (Type Name, Unit, Divisor)
    /// </summary>
    private static readonly Dictionary<byte, (string Type, string Unit, int Divisor)> SensorTypes = new()
    {
        // Umwelt-Sensoren
        [0x01] = ("temperature", "°C", 100),        // -327.68 bis 327.67 °C
        [0x02] = ("humidity", "%", 100),            // 0.00 bis 100.00 %
        [0x03] = ("pressure", "hPa", 10),           // 0.0 bis 6553.5 hPa
        [0x04] = ("water_level", "cm", 10),         // -3276.8 bis 3276.7 cm
        [0x05] = ("battery", "%", 100),             // 0.00 bis 100.00 %
        [0x06] = ("voltage", "V", 100),             // 0.00 bis 327.67 V

        // GPS-Daten (Spezialformat)
        [0x10] = ("latitude", "°", 10000),          // GPS Latitude
        [0x11] = ("longitude", "°", 10000),         // GPS Longitude
        [0x12] = ("altitude", "m", 10),             // GPS Altitude

        // Luftqualität
        [0x20] = ("pm25", "µg/m³", 1),              // Particulate Matter 2.5
        [0x21] = ("pm10", "µg/m³", 1),              // Particulate Matter 10
        [0x22] = ("co2", "ppm", 1),                 // CO2
        [0x23] = ("voc", "ppb", 1),                 // Volatile Organic Compounds

        // Licht & Strahlung
        [0x30] = ("light", "lux", 1),               // Helligkeit
        [0x31] = ("uv", "index", 100),              // UV-Index

        // Wetter
        [0x40] = ("wind_speed", "m/s", 100),        // Windgeschwindigkeit
        [0x41] = ("wind_direction", "°", 1),        // Windrichtung
        [0x42] = ("rainfall", "mm", 10),            // Niederschlag

        // Boden
        [0x50] = ("soil_moisture", "%", 100),       // Bodenfeuchtigkeit
        [0x51] = ("soil_temperature", "°C", 100),   // Bodentemperatur
        [0x52] = ("soil_ph", "pH", 100),            // Boden-pH

        // Wasser (Erft-Monitoring)
        [0x60] = ("water_temperature", "°C", 100),  // Wassertemperatur
        [0x61] = ("water_ph", "pH", 100),           // Wasser-pH
        [0x62] = ("water_conductivity", "µS/cm", 1),// Leitfähigkeit
        [0x63] = ("water_dissolved_oxygen", "mg/L", 100), // Gelöster Sauerstoff
        [0x64] = ("water_turbidity", "NTU", 10),    // Trübung
        [0x65] = ("water_flow", "m³/s", 1000),      // Durchfluss

        // System
        [0xF0] = ("rssi", "dBm", 1),                // Signalstärke (intern)
        [0xF1] = ("snr", "dB", 10),                 // Signal-to-Noise Ratio (intern)
        [0xFE] = ("error_code", "", 1),             // Fehlercode
        [0xFF] = ("status", "", 1),                 // Status-Code
    };

    public MyIoTGridDecoder(ILogger<MyIoTGridDecoder> logger)
    {
        _logger = logger;
    }

    public bool CanDecode(byte[] payload, int fPort)
    {
        // myIoTGrid Format: Payload muss Vielfaches von 3 sein
        // FPort 1-10 sind für Sensordaten reserviert
        return payload.Length > 0 &&
               payload.Length % 3 == 0 &&
               fPort >= 1 && fPort <= 10;
    }

    public IEnumerable<DecodedReading> Decode(byte[] payload, string devEui, int fPort)
    {
        if (payload == null || payload.Length == 0)
        {
            _logger.LogWarning("Empty payload for device {DevEui}", devEui);
            yield break;
        }

        if (payload.Length % 3 != 0)
        {
            _logger.LogWarning(
                "Invalid payload length: {Length} (must be multiple of 3) for device {DevEui}",
                payload.Length, devEui);
            yield break;
        }

        var timestamp = DateTime.UtcNow;

        // Decode all sensors in payload
        for (int i = 0; i + 2 < payload.Length; i += 3)
        {
            var typeCode = payload[i];

            // Signed 16-bit integer (Big Endian)
            var rawValue = (short)((payload[i + 1] << 8) | payload[i + 2]);

            if (SensorTypes.TryGetValue(typeCode, out var sensor))
            {
                var reading = new DecodedReading
                {
                    DevEui = devEui,
                    TypeCode = typeCode,
                    Type = sensor.Type,
                    Value = rawValue / (double)sensor.Divisor,
                    Unit = sensor.Unit,
                    Timestamp = timestamp,
                    Metadata = new Dictionary<string, string>
                    {
                        ["fPort"] = fPort.ToString(),
                        ["rawValue"] = rawValue.ToString(),
                        ["decoder"] = Name
                    }
                };

                _logger.LogDebug(
                    "Decoded {Type} = {Value} {Unit} from device {DevEui}",
                    reading.Type, reading.Value, reading.Unit, devEui);

                yield return reading;
            }
            else
            {
                _logger.LogWarning(
                    "Unknown sensor type: 0x{TypeCode:X2} for device {DevEui}",
                    typeCode, devEui);
            }
        }
    }

    /// <summary>
    /// Statische Methode zum Enkodieren eines Sensor-Werts
    /// Nützlich für Simulator und Tests
    /// </summary>
    public static byte[] Encode(string sensorType, double value)
    {
        var typeCode = SensorTypes
            .FirstOrDefault(x => x.Value.Type == sensorType);

        if (typeCode.Value.Type == null)
            throw new ArgumentException($"Unknown sensor type: {sensorType}");

        var rawValue = (short)(value * typeCode.Value.Divisor);

        return new byte[]
        {
            typeCode.Key,
            (byte)(rawValue >> 8),
            (byte)(rawValue & 0xFF)
        };
    }

    /// <summary>
    /// Statische Methode zum Enkodieren mehrerer Sensor-Werte
    /// </summary>
    public static byte[] EncodeMultiple(params (string SensorType, double Value)[] readings)
    {
        var result = new List<byte>();
        foreach (var (sensorType, value) in readings)
        {
            result.AddRange(Encode(sensorType, value));
        }
        return result.ToArray();
    }
}
