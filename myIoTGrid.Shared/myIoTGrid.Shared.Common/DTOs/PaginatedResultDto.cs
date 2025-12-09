namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO für paginierte Ergebnisse
/// </summary>
/// <typeparam name="T">Typ der Einträge</typeparam>
/// <param name="Items">Liste der Einträge</param>
/// <param name="TotalCount">Gesamtanzahl der Einträge</param>
/// <param name="Page">Aktuelle Seite (1-basiert)</param>
/// <param name="PageSize">Einträge pro Seite</param>
public record PaginatedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    /// <summary>Gesamtanzahl der Seiten</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Ob es eine nächste Seite gibt</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Ob es eine vorherige Seite gibt</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Erstellt ein leeres Ergebnis</summary>
    public static PaginatedResultDto<T> Empty(int page = 1, int pageSize = 50)
        => new(Array.Empty<T>(), 0, page, pageSize);
}
