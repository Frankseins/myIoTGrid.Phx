namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Source type for synced nodes (Domain)
/// </summary>
public enum SyncedNodeSource
{
    /// <summary>Direct node from another user</summary>
    Direct = 0,

    /// <summary>Virtual node (weather service, etc.)</summary>
    Virtual = 1,

    /// <summary>Node from another Hub</summary>
    OtherHub = 2
}
