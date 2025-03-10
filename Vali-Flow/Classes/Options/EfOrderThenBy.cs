using System.Linq.Expressions;

namespace Vali_Flow.Classes.Options;

/// <summary>
/// Represents a secondary ordering specification for Entity Framework queries using LINQ expressions.
/// </summary>
/// <typeparam name="T">The type of the entities to order.</typeparam>
/// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
public class EfOrderThenBy<T,TKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfOrderThenBy{T, TKey}"/> class with a secondary ordering expression.
    /// The ordering direction defaults to ascending.
    /// </summary>
    /// <param name="thenBy">A LINQ expression to extract the key for secondary ordering.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="thenBy"/> is null.</exception>
    public EfOrderThenBy(Expression<Func<T, TKey>> thenBy)
    {
        ThenBy = thenBy;
    }  
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EfOrderThenBy{T, TKey}"/> class with a secondary ordering expression
    /// and a specified ordering direction.
    /// </summary>
    /// <param name="thenBy">A LINQ expression to extract the key for secondary ordering.</param>
    /// <param name="ascending">If true, orders in ascending order; otherwise, descending.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="thenBy"/> is null.</exception>
    public EfOrderThenBy(Expression<Func<T, TKey>> thenBy, bool ascending)
    {
        ThenBy = thenBy;
        Ascending = ascending;
    }

    /// <summary>
    /// Gets or sets the LINQ expression used to extract the key for secondary ordering.
    /// </summary>
    public Expression<Func<T, TKey>> ThenBy { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the secondary ordering is in ascending order.
    /// Defaults to true (ascending).
    /// </summary>
    public bool Ascending { get; set; } = true;
}