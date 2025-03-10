namespace Vali_Flow.InMemory.Classes.Options;

/// <summary>
/// Represents a secondary ordering specification for in-memory evaluation of entities.
/// </summary>
/// <typeparam name="T">The type of the entities to order.</typeparam>
/// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
public class InMemoryThenBy<T,TKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryThenBy{T, TKey}"/> class with a secondary ordering function.
    /// The ordering direction defaults to ascending.
    /// </summary>
    /// <param name="thenBy">A function to extract the key for secondary ordering.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="thenBy"/> is null.</exception>
    public InMemoryThenBy(Func<T, TKey> thenBy)
    {
        ThenBy = thenBy;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryThenBy{T, TKey}"/> class with a secondary ordering function
    /// and a specified ordering direction.
    /// </summary>
    /// <param name="thenBy">A function to extract the key for secondary ordering.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="thenBy"/> is null.</exception>
    public InMemoryThenBy(Func<T, TKey> thenBy,bool ascending)
    {
        ThenBy = thenBy;
        Ascending = ascending;
    }

    /// <summary>
    /// Gets or sets the function used to extract the key for secondary ordering.
    /// </summary>
    public Func<T, TKey> ThenBy { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the secondary ordering is in ascending order.
    /// Defaults to true (ascending).
    /// </summary>
    public bool Ascending { get; set; } = true;
}