using CleanArchTemplate.Shared.Models;

namespace CleanArchTemplate.Shared.Extensions;

/// <summary>
/// Extension methods for IEnumerable operations
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Checks if an enumerable is null or empty
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">The enumerable to check</param>
    /// <returns>True if the enumerable is null or empty; otherwise, false</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source == null || !source.Any();
    }

    /// <summary>
    /// Checks if an enumerable has any elements
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">The enumerable to check</param>
    /// <returns>True if the enumerable has elements; otherwise, false</returns>
    public static bool HasAny<T>(this IEnumerable<T>? source)
    {
        return source != null && source.Any();
    }

    /// <summary>
    /// Safely converts an enumerable to a list, returning an empty list if null
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">The enumerable to convert</param>
    /// <returns>A list containing the elements, or an empty list if source is null</returns>
    public static List<T> SafeToList<T>(this IEnumerable<T>? source)
    {
        return source?.ToList() ?? new List<T>();
    }

    /// <summary>
    /// Safely converts an enumerable to an array, returning an empty array if null
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">The enumerable to convert</param>
    /// <returns>An array containing the elements, or an empty array if source is null</returns>
    public static T[] SafeToArray<T>(this IEnumerable<T>? source)
    {
        return source?.ToArray() ?? Array.Empty<T>();
    }

    /// <summary>
    /// Performs an action on each element in the enumerable
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">The enumerable</param>
    /// <param name="action">The action to perform</param>
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
    /// Splits an enumerable into batches of a specified size
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">The enumerable to split</param>
    /// <param name="batchSize">The size of each batch</param>
    /// <returns>An enumerable of batches</returns>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));

        return BatchIterator(source, batchSize);
    }

    private static IEnumerable<IEnumerable<T>> BatchIterator<T>(IEnumerable<T> source, int batchSize)
    {
        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return GetBatch(enumerator, batchSize);
        }
    }

    private static IEnumerable<T> GetBatch<T>(IEnumerator<T> enumerator, int batchSize)
    {
        var count = 0;
        do
        {
            yield return enumerator.Current;
            count++;
        } while (count < batchSize && enumerator.MoveNext());
    }

    /// <summary>
    /// Creates a paginated result from an enumerable
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">The enumerable to paginate</param>
    /// <param name="pageNumber">The page number</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A paginated result</returns>
    public static PaginatedResult<T> ToPaginatedResult<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        var list = source.ToList();
        var totalCount = list.Count;
        var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        return new PaginatedResult<T>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Removes null values from an enumerable
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">The enumerable</param>
    /// <returns>An enumerable without null values</returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        return source.Where(x => x != null)!;
    }

    /// <summary>
    /// Gets distinct elements by a specified key selector
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <param name="source">The enumerable</param>
    /// <param name="keySelector">The key selector function</param>
    /// <returns>Distinct elements by the specified key</returns>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

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