using System.Text.Json.Serialization;
using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for Node Debug Log entries (Sprint 8: Remote Debug System).
/// </summary>
public record NodeDebugLogDto(
    Guid Id,
    Guid NodeId,
    long NodeTimestamp,
    DateTime ReceivedAt,
    DebugLevelDto Level,
    LogCategoryDto Category,
    string Message,
    string? StackTrace
);

/// <summary>
/// DTO for creating debug log entries (received from firmware).
/// Accepts both "timestamp" (ESP32) and "nodeTimestamp" (standard) field names.
/// Accepts both string and integer values for Level and Category.
/// </summary>
public class CreateNodeDebugLogDto
{
    [JsonPropertyName("nodeTimestamp")]
    public long NodeTimestamp { get; set; }

    /// <summary>
    /// Alternative field name sent by ESP32 firmware
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long? Timestamp
    {
        get => null;
        set { if (value.HasValue) NodeTimestamp = value.Value; }
    }

    [JsonPropertyName("level")]
    [JsonConverter(typeof(DebugLevelStringConverter))]
    public DebugLevelDto Level { get; set; }

    [JsonPropertyName("category")]
    [JsonConverter(typeof(LogCategoryStringConverter))]
    public LogCategoryDto Category { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }
}

/// <summary>
/// DTO for batch debug log upload from firmware.
/// Accepts both "nodeId" (standard) and "serialNumber" (ESP32) field names.
/// </summary>
public class DebugLogBatchDto
{
    /// <summary>
    /// Node identifier (can be NodeId string or MAC address)
    /// </summary>
    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Alternative field name sent by ESP32 firmware (maps to NodeId)
    /// </summary>
    [JsonPropertyName("serialNumber")]
    public string? SerialNumber
    {
        get => null;
        set { if (!string.IsNullOrEmpty(value)) NodeId = value; }
    }

    [JsonPropertyName("logs")]
    public List<CreateNodeDebugLogDto> Logs { get; set; } = new();
}

/// <summary>
/// DTO for setting node debug level.
/// </summary>
public record SetNodeDebugLevelDto(
    DebugLevelDto DebugLevel,
    bool EnableRemoteLogging
);

/// <summary>
/// Response DTO for debug level configuration.
/// </summary>
public record NodeDebugConfigurationDto(
    Guid NodeId,
    string SerialNumber,
    DebugLevelDto DebugLevel,
    bool EnableRemoteLogging,
    DateTime? LastDebugChange
);

/// <summary>
/// DTO for debug log filter options.
/// </summary>
public record DebugLogFilterDto(
    Guid NodeId,
    DebugLevelDto? MinLevel = null,
    LogCategoryDto? Category = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 100
);

/// <summary>
/// DTO for error statistics per node.
/// </summary>
public record NodeErrorStatisticsDto(
    Guid NodeId,
    string NodeName,
    int TotalLogs,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    Dictionary<string, int> ErrorsByCategory,
    DateTime? LastErrorAt,
    string? LastErrorMessage
);

/// <summary>
/// DTO for debug log cleanup response.
/// </summary>
public record DebugLogCleanupResultDto(
    int DeletedCount,
    DateTime CleanupBefore
);

/// <summary>
/// DTO for raw serial output from firmware (Remote Serial Monitor).
/// Contains raw serial lines exactly as they appear in the ESP32 serial monitor.
/// </summary>
public class SerialOutputBatchDto
{
    /// <summary>
    /// Node serial number (e.g., "ESP32-0070078492CC")
    /// </summary>
    [JsonPropertyName("serialNumber")]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp from ESP32 (millis())
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Raw serial output lines
    /// </summary>
    [JsonPropertyName("lines")]
    public List<string> Lines { get; set; } = new();
}

/// <summary>
/// DTO for a single serial output line (for SignalR broadcast).
/// </summary>
public record SerialOutputLineDto(
    Guid NodeId,
    string NodeName,
    string Line,
    DateTime ReceivedAt
);
