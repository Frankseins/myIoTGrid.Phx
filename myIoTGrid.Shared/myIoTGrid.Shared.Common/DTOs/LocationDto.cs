namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO für Standort-Informationen
/// </summary>
/// <param name="Name">Name des Standorts (z.B. "Wohnzimmer")</param>
/// <param name="Latitude">Breitengrad für GPS-Koordinaten</param>
/// <param name="Longitude">Längengrad für GPS-Koordinaten</param>
public record LocationDto(
    string? Name = null,
    double? Latitude = null,
    double? Longitude = null
)
{
    /// <summary>
    /// Prüft ob GPS-Koordinaten vorhanden sind
    /// </summary>
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
}
