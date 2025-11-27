using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions für SensorType
/// </summary>
public static class SensorTypeMappingExtensions
{
    /// <summary>
    /// Konvertiert SensorType Entity zu SensorTypeDto
    /// </summary>
    public static SensorTypeDto ToDto(this SensorType entity)
    {
        return new SensorTypeDto(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Unit,
            entity.Description,
            entity.IconName,
            entity.IsGlobal,
            entity.CreatedAt
        );
    }

    /// <summary>
    /// Konvertiert CreateSensorTypeDto zu SensorType Entity
    /// </summary>
    public static SensorType ToEntity(this CreateSensorTypeDto dto)
    {
        return new SensorType
        {
            Id = Guid.NewGuid(),
            Code = dto.Code.ToLowerInvariant(),
            Name = dto.Name,
            Unit = dto.Unit,
            Description = dto.Description,
            IconName = dto.IconName,
            IsGlobal = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Konvertiert eine Liste von SensorType Entities zu DTOs
    /// </summary>
    public static IEnumerable<SensorTypeDto> ToDtos(this IEnumerable<SensorType> entities)
    {
        return entities.Select(e => e.ToDto());
    }
}
