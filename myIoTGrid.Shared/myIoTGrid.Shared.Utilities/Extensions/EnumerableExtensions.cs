namespace myIoTGrid.Shared.Utilities.Extensions;

/// <summary>
/// Extension methods for IEnumerable operations
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Checks if a collection is null or empty
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">The collection to check</param>
    /// <returns>True if null or empty</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
        => source == null || !source.Any();

    /// <summary>
    /// Checks if a collection has any elements
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">The collection to check</param>
    /// <returns>True if has elements</returns>
    public static bool HasItems<T>(this IEnumerable<T>? source)
        => source != null && source.Any();

    /// <summary>
    /// Returns an empty enumerable if source is null
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">The collection</param>
    /// <returns>Source or empty enumerable</returns>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
        => source ?? Enumerable.Empty<T>();

    /// <summary>
    /// Splits a collection into batches of specified size
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">The collection to batch</param>
    /// <param name="batchSize">Size of each batch</param>
    /// <returns>Enumerable of batches</returns>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

        return source
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.item));
    }

    /// <summary>
    /// Executes an action for each element
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">The collection</param>
    /// <param name="action">Action to execute</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Executes an async action for each element
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">The collection</param>
    /// <param name="action">Async action to execute</param>
    /// <param name="ct">Cancellation token</param>
    public static async Task ForEachAsync<T>(
        this IEnumerable<T> source,
        Func<T, CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source)
        {
            ct.ThrowIfCancellationRequested();
            await action(item, ct);
        }
    }

    /// <summary>
    /// Filters out null values
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <param name="source">The collection</param>
    /// <returns>Collection without nulls</returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        => source.Where(x => x != null)!;

    /// <summary>
    /// Distinct by a key selector
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <param name="source">The collection</param>
    /// <param name="keySelector">Key selector function</param>
    /// <returns>Distinct elements by key</returns>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector)
    {
        var seenKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            if (seenKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }
}
