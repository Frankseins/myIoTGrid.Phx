using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for SensorType Entity.
/// Matter-konform: Entspricht einem Matter Cluster.
/// </summary>
public static class SensorTypeMappingExtensions
{
    /// <summary>
    /// Converts a SensorType entity to a SensorTypeDto
    /// </summary>
    public static SensorTypeDto ToDto(this SensorType sensorType)
    {
        return new SensorTypeDto(
            TypeId: sensorType.TypeId,
            DisplayName: sensorType.DisplayName,
            ClusterId: sensorType.ClusterId,
            MatterClusterName: sensorType.MatterClusterName,
            Unit: sensorType.Unit,
            Resolution: sensorType.Resolution,
            MinValue: sensorType.MinValue,
            MaxValue: sensorType.MaxValue,
            Description: sensorType.Description,
            IsCustom: sensorType.IsCustom,
            Category: sensorType.Category,
            Icon: sensorType.Icon,
            Color: sensorType.Color,
            IsGlobal: sensorType.IsGlobal,
            CreatedAt: sensorType.CreatedAt
        );
    }

    /// <summary>
    /// Converts a CreateSensorTypeDto to a SensorType entity
    /// </summary>
    public static SensorType ToEntity(this CreateSensorTypeDto dto)
    {
        return new SensorType
        {
            TypeId = dto.TypeId.ToLowerInvariant(),
            DisplayName = dto.DisplayName,
            ClusterId = dto.ClusterId,
            MatterClusterName = dto.MatterClusterName,
            Unit = dto.Unit,
            Resolution = dto.Resolution,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            Description = dto.Description,
            IsCustom = dto.IsCustom,
            Category = dto.Category,
            Icon = dto.Icon,
            Color = dto.Color,
            IsGlobal = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts a list of SensorType Entities to DTOs
    /// </summary>
    public static IEnumerable<SensorTypeDto> ToDtos(this IEnumerable<SensorType> sensorTypes)
    {
        return sensorTypes.Select(st => st.ToDto());
    }
}
