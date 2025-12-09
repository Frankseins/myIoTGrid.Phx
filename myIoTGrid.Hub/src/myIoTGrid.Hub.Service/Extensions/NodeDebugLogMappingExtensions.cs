
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for NodeDebugLog Entity (Sprint 8: Remote Debug System).
/// </summary>
public static class NodeDebugLogMappingExtensions
{
    /// <summary>
    /// Converts a NodeDebugLog entity to a NodeDebugLogDto
    /// </summary>
    public static NodeDebugLogDto ToDto(this NodeDebugLog log)
    {
        return new NodeDebugLogDto(
            Id: log.Id,
            NodeId: log.NodeId,
            NodeTimestamp: log.NodeTimestamp,
            ReceivedAt: log.ReceivedAt,
            Level: log.Level.ToDto(),
            Category: log.Category.ToDto(),
            Message: log.Message,
            StackTrace: log.StackTrace
        );
    }

    /// <summary>
    /// Converts a CreateNodeDebugLogDto to a NodeDebugLog entity
    /// </summary>
    public static NodeDebugLog ToEntity(this CreateNodeDebugLogDto dto, Guid nodeId)
    {
        return new NodeDebugLog
        {
            Id = Guid.NewGuid(),
            NodeId = nodeId,
            NodeTimestamp = dto.NodeTimestamp,
            ReceivedAt = DateTime.UtcNow,
            Level = dto.Level.ToEntity(),
            Category = dto.Category.ToEntity(),
            Message = dto.Message,
            StackTrace = dto.StackTrace
        };
    }

    /// <summary>
    /// Converts LogCategory enum to LogCategoryDto
    /// </summary>
    public static LogCategoryDto ToDto(this LogCategory category)
    {
        return category switch
        {
            LogCategory.System => LogCategoryDto.System,
            LogCategory.Hardware => LogCategoryDto.Hardware,
            LogCategory.Network => LogCategoryDto.Network,
            LogCategory.Sensor => LogCategoryDto.Sensor,
            LogCategory.GPS => LogCategoryDto.GPS,
            LogCategory.API => LogCategoryDto.API,
            LogCategory.Storage => LogCategoryDto.Storage,
            LogCategory.Error => LogCategoryDto.Error,
            _ => LogCategoryDto.System
        };
    }

    /// <summary>
    /// Converts LogCategoryDto to LogCategory enum
    /// </summary>
    public static LogCategory ToEntity(this LogCategoryDto category)
    {
        return category switch
        {
            LogCategoryDto.System => LogCategory.System,
            LogCategoryDto.Hardware => LogCategory.Hardware,
            LogCategoryDto.Network => LogCategory.Network,
            LogCategoryDto.Sensor => LogCategory.Sensor,
            LogCategoryDto.GPS => LogCategory.GPS,
            LogCategoryDto.API => LogCategory.API,
            LogCategoryDto.Storage => LogCategory.Storage,
            LogCategoryDto.Error => LogCategory.Error,
            _ => LogCategory.System
        };
    }

    /// <summary>
    /// Creates a NodeDebugConfigurationDto from a Node entity
    /// </summary>
    public static NodeDebugConfigurationDto ToDebugConfigDto(this Node node)
    {
        return new NodeDebugConfigurationDto(
            NodeId: node.Id,
            SerialNumber: node.MacAddress,
            DebugLevel: node.DebugLevel.ToDto(),
            EnableRemoteLogging: node.EnableRemoteLogging,
            LastDebugChange: node.LastDebugChange
        );
    }
}
