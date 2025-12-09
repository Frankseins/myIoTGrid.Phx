
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions für Location
/// </summary>
public static class LocationMappingExtensions
{
    /// <summary>
    /// Konvertiert LocationDto zu Domain Location
    /// </summary>
    public static Location? ToEntity(this LocationDto? dto)
    {
        if (dto == null) return null;

        return new Location(dto.Name, dto.Latitude, dto.Longitude);
    }

    /// <summary>
    /// Konvertiert Domain Location zu LocationDto
    /// </summary>
    public static LocationDto? ToDto(this Location? entity)
    {
        if (entity == null) return null;

        return new LocationDto(entity.Name, entity.Latitude, entity.Longitude);
    }
}
