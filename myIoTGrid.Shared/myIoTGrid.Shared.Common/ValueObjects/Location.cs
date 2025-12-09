namespace myIoTGrid.Shared.Common.ValueObjects;

/// <summary>
/// Value Object für Standort-Informationen
/// </summary>
public class Location
{
    /// <summary>Name des Standorts (z.B. "Wohnzimmer")</summary>
    public string? Name { get; set; }

    /// <summary>Breitengrad für GPS-Koordinaten</summary>
    public double? Latitude { get; set; }

    /// <summary>Längengrad für GPS-Koordinaten</summary>
    public double? Longitude { get; set; }

    public Location()
    {
    }

    public Location(string? name, double? latitude = null, double? longitude = null)
    {
        Name = name;
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Prüft ob GPS-Koordinaten vorhanden sind
    /// </summary>
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

    /// <summary>
    /// Erstellt eine Kopie des Location-Objekts
    /// </summary>
    public Location Clone() => new(Name, Latitude, Longitude);

    public override string ToString()
    {
        if (HasCoordinates)
            return $"{Name ?? "Unknown"} ({Latitude:F6}, {Longitude:F6})";
        return Name ?? "Unknown";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Location other) return false;
        return Name == other.Name &&
               Latitude == other.Latitude &&
               Longitude == other.Longitude;
    }

    public override int GetHashCode() => HashCode.Combine(Name, Latitude, Longitude);
}
