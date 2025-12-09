namespace myIoTGrid.Shared.Common.DTOs.Common;

/// <summary>
/// Standardisiertes Paginierungs-Ergebnis
/// </summary>
public record PagedResultDto<T>
{
    /// <summary>Die Datensätze der aktuellen Seite</summary>
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();

    /// <summary>Gesamtanzahl aller Datensätze</summary>
    public int TotalRecords { get; init; }

    /// <summary>Aktuelle Seitennummer (0-basiert)</summary>
    public int Page { get; init; }

    /// <summary>Einträge pro Seite</summary>
    public int Size { get; init; }

    /// <summary>Gesamtanzahl der Seiten</summary>
    public int TotalPages => Size > 0 ? (int)Math.Ceiling((double)TotalRecords / Size) : 0;

    /// <summary>Gibt es eine nächste Seite?</summary>
    public bool HasNextPage => Page < TotalPages - 1;

    /// <summary>Gibt es eine vorherige Seite?</summary>
    public bool HasPreviousPage => Page > 0;

    /// <summary>
    /// Erstellt ein PagedResultDto aus Items und Query-Parametern
    /// </summary>
    public static PagedResultDto<T> Create(
        IEnumerable<T> items,
        int totalRecords,
        QueryParamsDto query)
    {
        return new PagedResultDto<T>
        {
            Items = items,
            TotalRecords = totalRecords,
            Page = query.Page,
            Size = query.Size
        };
    }

    /// <summary>
    /// Erstellt ein leeres PagedResultDto
    /// </summary>
    public static PagedResultDto<T> Empty(QueryParamsDto query)
    {
        return new PagedResultDto<T>
        {
            Items = Enumerable.Empty<T>(),
            TotalRecords = 0,
            Page = query.Page,
            Size = query.Size
        };
    }
}
