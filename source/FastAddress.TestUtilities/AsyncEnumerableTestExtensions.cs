namespace FastAddress.TestUtilities;

/// <summary>
/// Helpers for working with <see cref="IAsyncEnumerable{T}"/> in tests without depending on
/// a specific LINQ-async provider.
/// </summary>
public static class AsyncEnumerableTestExtensions
{
    /// <summary>
    /// Materialize an async sequence into a list.
    /// </summary>
    public static async Task<List<T>> CollectAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var items = new List<T>();
        await foreach (var item in source.WithCancellation(ct))
        {
            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// Wrap a synchronous sequence as an <see cref="IAsyncEnumerable{T}"/> for feeding mocked async APIs.
    /// </summary>
    public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            yield return item;
            await Task.CompletedTask;
        }
    }
}
