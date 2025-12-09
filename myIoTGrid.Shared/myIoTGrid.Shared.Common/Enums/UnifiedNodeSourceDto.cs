namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Source of a unified Node (DTO)
/// </summary>
public enum UnifiedNodeSourceDto
{
    /// <summary>Node connected to this Hub</summary>
    Local = 0,

    /// <summary>ESP32 directly connected to Cloud (not via this Hub)</summary>
    Direct = 1,

    /// <summary>External data source (DWD, Sensor.Community, etc.)</summary>
    Virtual = 2,

    /// <summary>Node from another Hub in the same tenant</summary>
    OtherHub = 3
}
