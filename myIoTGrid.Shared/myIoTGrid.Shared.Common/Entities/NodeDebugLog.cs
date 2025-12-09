using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Represents a debug log entry received from a node (Sprint 8: Remote Debug System).
/// Stores timestamped log messages with level and category for troubleshooting.
/// </summary>
public class NodeDebugLog : IEntity
{
    /// <summary>Primary key</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the Node that generated this log</summary>
    public Guid NodeId { get; set; }

    /// <summary>Timestamp when the log was generated on the node (millis since boot)</summary>
    public long NodeTimestamp { get; set; }

    /// <summary>Timestamp when the log was received by the Hub</summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>Debug level of this log entry</summary>
    public DebugLevel Level { get; set; }

    /// <summary>Category of the log entry</summary>
    public LogCategory Category { get; set; }

    /// <summary>Log message content</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Optional stack trace for error logs</summary>
    public string? StackTrace { get; set; }

    // === Navigation Properties ===

    /// <summary>Node that generated this log</summary>
    public Node? Node { get; set; }
}
