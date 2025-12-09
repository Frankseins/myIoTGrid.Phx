using System.Text.Json;
using System.Text.Json.Serialization;

namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Debug level for node logging (Sprint 8: Remote Debug System).
/// Controls verbosity of debug output and performance impact.
/// </summary>
public enum DebugLevelDto
{
    /// <summary>Minimal logging, errors only. Best performance.</summary>
    Production = 0,

    /// <summary>Standard logging for normal operation. Default level.</summary>
    Normal = 1,

    /// <summary>Verbose logging for troubleshooting. Higher battery/performance impact.</summary>
    Debug = 2
}

/// <summary>
/// JSON converter that accepts both string and integer values for DebugLevelDto.
/// ESP32 sends string values like "Debug", "Normal", "Production".
/// </summary>
public class DebugLevelStringConverter : JsonConverter<DebugLevelDto>
{
    public override DebugLevelDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return (DebugLevelDto)reader.GetInt32();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (Enum.TryParse<DebugLevelDto>(str, true, out var result))
            {
                return result;
            }
        }

        return DebugLevelDto.Normal; // Default
    }

    public override void Write(Utf8JsonWriter writer, DebugLevelDto value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
