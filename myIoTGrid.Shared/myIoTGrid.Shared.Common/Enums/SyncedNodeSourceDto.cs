namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Source of a synchronized Node (DTO)
/// </summary>
public enum SyncedNodeSourceDto
{
    /// <summary>ESP32 directly connected to Cloud (not via this Hub)</summary>
    Direct = 0,

    /// <summary>External data source (DWD, Sensor.Community, etc.)</summary>
    Virtual = 1,

    /// <summary>Node from another Hub in the same tenant</summary>
    OtherHub = 2
}
