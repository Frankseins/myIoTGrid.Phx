using System.Text.Json;
using System.Text.Json.Serialization;

namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Log category for categorizing debug logs (Sprint 8: Remote Debug System).
/// Used for filtering logs by subsystem.
/// </summary>
public enum LogCategoryDto
{
    /// <summary>System/boot/state machine logs</summary>
    System = 0,

    /// <summary>Hardware/I2C/UART/GPIO logs</summary>
    Hardware = 1,

    /// <summary>WiFi/BLE/connectivity logs</summary>
    Network = 2,

    /// <summary>Sensor reading/measurement logs</summary>
    Sensor = 3,

    /// <summary>GPS/GNSS specific logs</summary>
    GPS = 4,

    /// <summary>HTTP API/Hub communication logs</summary>
    API = 5,

    /// <summary>SD card/NVS/storage logs</summary>
    Storage = 6,

    /// <summary>Error conditions (always logged)</summary>
    Error = 7
}

/// <summary>
/// JSON converter that accepts both string and integer values for LogCategoryDto.
/// ESP32 sends string values like "System", "Hardware", "Network", etc.
/// </summary>
public class LogCategoryStringConverter : JsonConverter<LogCategoryDto>
{
    public override LogCategoryDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return (LogCategoryDto)reader.GetInt32();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (Enum.TryParse<LogCategoryDto>(str, true, out var result))
            {
                return result;
            }
        }

        return LogCategoryDto.System; // Default
    }

    public override void Write(Utf8JsonWriter writer, LogCategoryDto value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
