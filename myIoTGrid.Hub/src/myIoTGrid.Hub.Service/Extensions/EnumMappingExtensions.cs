
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions für Enums zwischen Domain und Shared
/// </summary>
public static class EnumMappingExtensions
{
    // Protocol Mapping
    public static ProtocolDto ToDto(this Protocol protocol) => (ProtocolDto)protocol;
    public static Protocol ToEntity(this ProtocolDto protocol) => (Protocol)protocol;

    // AlertLevel Mapping
    public static AlertLevelDto ToDto(this AlertLevel level) => (AlertLevelDto)level;
    public static AlertLevel ToEntity(this AlertLevelDto level) => (AlertLevel)level;

    // AlertSource Mapping
    public static AlertSourceDto ToDto(this AlertSource source) => (AlertSourceDto)source;
    public static AlertSource ToEntity(this AlertSourceDto source) => (AlertSource)source;

    // StorageMode Mapping (Sprint OS-01)
    public static StorageModeDto ToDto(this StorageMode mode) => (StorageModeDto)mode;
    public static StorageMode ToEntity(this StorageModeDto mode) => (StorageMode)mode;
}
