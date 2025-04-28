using System.Linq.Expressions;
using Vali_Flow.Interfaces.Options;

namespace Vali_Flow.Classes.Options;

/// <summary>
/// Represents a secondary ordering expression (ThenBy) for a specific property of type TKey.
/// </summary>
/// <typeparam name="T">The type of the entity being ordered.</typeparam>
/// <typeparam name="TProperty">The type of the key used for ordering, which must be non-nullable.</typeparam>
public sealed class EfOrderThenBy<T,TProperty> : IEfOrderThenBy<T> where TProperty : notnull
{
    private readonly Expression<Func<T, TProperty>> _expression;
    private readonly bool _ascending;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfOrderThenBy{T, TProperty}"/> class.
    /// </summary>
    /// <param name="expression">The expression that defines the property to order by. Cannot be null.</param>
    /// <param name="ascending">A value indicating whether to order in ascending order. Default is true.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="expression"/> parameter is null.</exception>
    public EfOrderThenBy(Expression<Func<T, TProperty>> expression, bool ascending = true)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _ascending = ascending;
    }

    /// <summary>
    /// Applies the secondary ordering to the ordered query.
    /// </summary>
    /// <param name="query">The ordered query to which the secondary ordering will be applied.</param>
    /// <returns>The updated ordered query with the secondary ordering applied.</returns>
    public IOrderedQueryable<T> ApplyThenBy(IOrderedQueryable<T> query)
    {
        return _ascending
            ? query.ThenBy(_expression)
            : query.ThenByDescending(_expression);
    }
}