
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Alert
/// </summary>
public static class AlertMappingExtensions
{
    /// <summary>
    /// Converts Alert Entity to AlertDto
    /// </summary>
    public static AlertDto ToDto(this Alert entity, AlertType alertType, string? hubName = null, string? nodeName = null)
    {
        return new AlertDto(
            entity.Id,
            entity.TenantId,
            entity.HubId,
            hubName,
            entity.NodeId,
            nodeName,
            entity.AlertTypeId,
            alertType.Code,
            alertType.Name,
            entity.Level.ToDto(),
            entity.Message,
            entity.Recommendation,
            entity.Source.ToDto(),
            entity.CreatedAt,
            entity.ExpiresAt,
            entity.AcknowledgedAt,
            entity.IsActive
        );
    }

    /// <summary>
    /// Converts Alert Entity to AlertDto (when AlertType, Hub and Node are loaded)
    /// </summary>
    public static AlertDto ToDto(this Alert entity)
    {
        if (entity.AlertType == null)
            throw new InvalidOperationException("AlertType must be loaded to convert to DTO");

        return entity.ToDto(
            entity.AlertType,
            entity.Hub?.Name,
            entity.Node?.Name
        );
    }

    /// <summary>
    /// Converts CreateAlertDto to Alert Entity
    /// </summary>
    public static Alert ToEntity(
        this CreateAlertDto dto,
        Guid tenantId,
        Guid alertTypeId,
        Guid? hubId,
        Guid? nodeId,
        AlertSource source)
    {
        return new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HubId = hubId,
            NodeId = nodeId,
            AlertTypeId = alertTypeId,
            Level = dto.Level.ToEntity(),
            Message = dto.Message,
            Recommendation = dto.Recommendation,
            Source = source,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt,
            IsActive = true
        };
    }

    /// <summary>
    /// Converts a list of Alert Entities to DTOs
    /// </summary>
    public static IEnumerable<AlertDto> ToDtos(this IEnumerable<Alert> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}
