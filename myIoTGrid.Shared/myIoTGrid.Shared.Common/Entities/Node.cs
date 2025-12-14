using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Common.Interfaces;
using myIoTGrid.Shared.Common.ValueObjects;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Represents a physical IoT device (ESP32, LoRa32).
/// Matter-konform: Corresponds to a Matter Node.
/// One Hub can manage multiple Nodes.
/// Supports self-provisioning via BLE pairing.
/// </summary>
public class Node : IEntity
{
    /// <summary>Primary key (internal)</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the Hub managing this node</summary>
    public Guid HubId { get; set; }

    /// <summary>Unique device identifier (e.g., "wetterstation-garten-01")</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>Display name (e.g., "Wetterstation Garten")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Communication protocol (WLAN, LoRaWAN)</summary>
    public Protocol Protocol { get; set; } = Protocol.WLAN;

    /// <summary>Physical location of the node</summary>
    public Location? Location { get; set; }

    /// <summary>Firmware version (if reported)</summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>Battery level 0-100% (for battery-powered devices)</summary>
    public int? BatteryLevel { get; set; }

    /// <summary>Last contact timestamp</summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>Online status</summary>
    public bool IsOnline { get; set; }

    /// <summary>When the node was first registered</summary>
    public DateTime CreatedAt { get; set; }

    // === Node Provisioning ===

    /// <summary>MAC address of the device (unique identifier from hardware)</summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>SHA256 hash of the API key for authentication</summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>Current provisioning status of the node</summary>
    public NodeStatus Status { get; set; } = NodeStatus.Unconfigured;

    /// <summary>Whether this node generates simulated sensor values</summary>
    public bool IsSimulation { get; set; } = false;

    // === Offline Storage (Sprint OS-01) ===

    /// <summary>Storage mode for sensor readings</summary>
    public StorageMode StorageMode { get; set; } = StorageMode.RemoteOnly;

    /// <summary>Number of readings pending sync on the device</summary>
    public int PendingSyncCount { get; set; } = 0;

    /// <summary>Last successful sync timestamp</summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>Last sync error message (null if no error)</summary>
    public string? LastSyncError { get; set; }

    // === Remote Debug System (Sprint 8) ===

    /// <summary>Current debug level for this node</summary>
    public DebugLevel DebugLevel { get; set; } = DebugLevel.Normal;

    /// <summary>Whether remote logging is enabled (logs sent to Hub)</summary>
    public bool EnableRemoteLogging { get; set; } = false;

    /// <summary>When debug settings were last changed</summary>
    public DateTime? LastDebugChange { get; set; }

    // === Hardware Status (Sprint 8) ===

    /// <summary>Last hardware status report as JSON</summary>
    public string? HardwareStatusJson { get; set; }

    /// <summary>When hardware status was last reported</summary>
    public DateTime? HardwareStatusReportedAt { get; set; }

    // === Navigation Properties ===

    /// <summary>Hub managing this node</summary>
    public Hub? Hub { get; set; }

    /// <summary>Sensor assignments on this node</summary>
    public ICollection<NodeSensorAssignment> SensorAssignments { get; set; } = new List<NodeSensorAssignment>();

    /// <summary>Readings from this node</summary>
    public ICollection<Reading> Readings { get; set; } = new List<Reading>();

    /// <summary>Alerts for this node</summary>
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    /// <summary>Debug logs from this node (Sprint 8)</summary>
    public ICollection<NodeDebugLog> DebugLogs { get; set; } = new List<NodeDebugLog>();
}
