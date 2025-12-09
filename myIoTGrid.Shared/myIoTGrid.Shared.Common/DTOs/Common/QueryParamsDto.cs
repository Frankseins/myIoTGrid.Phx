namespace myIoTGrid.Shared.Common.DTOs.Common;

/// <summary>
/// Standard Query Parameters für alle Listen-Abfragen
/// </summary>
public record QueryParamsDto
{
    /// <summary>Aktuelle Seitennummer (0-basiert)</summary>
    public int Page { get; init; } = 0;

    /// <summary>Einträge pro Seite</summary>
    public int Size { get; init; } = 10;

    /// <summary>Sortierung (z.B. "name,asc" oder "createdAt,desc")</summary>
    public string? Sort { get; init; }

    /// <summary>Globaler Suchbegriff</summary>
    public string? Search { get; init; }

    /// <summary>Startdatum für Zeitfilter (ISO-Format)</summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>Enddatum für Zeitfilter (ISO-Format)</summary>
    public DateTime? DateTo { get; init; }

    /// <summary>Zusätzliche Filter als Key-Value-Paare</summary>
    public Dictionary<string, string>? Filters { get; init; }

    // Berechnete Properties
    public int Skip => Page * Size;
    public int Take => Size;

    /// <summary>
    /// Parst die Sort-Property in Feldname und Richtung
    /// </summary>
    public (string Field, bool Ascending) ParseSort()
    {
        if (string.IsNullOrWhiteSpace(Sort))
            return ("Id", true);

        var parts = Sort.Split(',');
        var field = parts[0].Trim();
        var ascending = parts.Length < 2 ||
                        !parts[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (field, ascending);
    }
}
