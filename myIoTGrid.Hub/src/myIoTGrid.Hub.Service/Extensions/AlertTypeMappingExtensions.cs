
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions für AlertType
/// </summary>
public static class AlertTypeMappingExtensions
{
    /// <summary>
    /// Konvertiert AlertType Entity zu AlertTypeDto
    /// </summary>
    public static AlertTypeDto ToDto(this AlertType entity)
    {
        return new AlertTypeDto(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Description,
            entity.DefaultLevel.ToDto(),
            entity.IconName,
            entity.IsGlobal,
            entity.CreatedAt
        );
    }

    /// <summary>
    /// Konvertiert CreateAlertTypeDto zu AlertType Entity
    /// </summary>
    public static AlertType ToEntity(this CreateAlertTypeDto dto)
    {
        return new AlertType
        {
            Id = Guid.NewGuid(),
            Code = dto.Code.ToLowerInvariant(),
            Name = dto.Name,
            Description = dto.Description,
            DefaultLevel = dto.DefaultLevel.ToEntity(),
            IconName = dto.IconName,
            IsGlobal = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Konvertiert eine Liste von AlertType Entities zu DTOs
    /// </summary>
    public static IEnumerable<AlertTypeDto> ToDtos(this IEnumerable<AlertType> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}
