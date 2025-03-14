using System.Linq.Expressions;
using Vali_Flow.Interfaces.Options;

namespace Vali_Flow.Classes.Options;

/// <summary>
/// Represents a primary ordering expression (OrderBy) for a specific property of type TKey.
/// </summary>
/// <typeparam name="T">The type of the entity being ordered.</typeparam>
/// <typeparam name="TProperty">The type of the key used for ordering, which must be non-nullable.</typeparam>
public class EfOrderBy<T,TProperty> : IEfOrderBy<T> where TProperty : notnull
{
    private readonly Expression<Func<T, TProperty>> _expression;
    private readonly bool _ascending;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EfOrderBy{T, TProperty}"/> class.
    /// </summary>
    /// <param name="expression">The expression that defines the property to order by. Cannot be null.</param>
    /// <param name="ascending">A value indicating whether to order in ascending order. Default is true.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="expression"/> parameter is null.</exception>
    public EfOrderBy(Expression<Func<T, TProperty>> expression, bool ascending = true)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _ascending = ascending;
    }
    
    /// <summary>
    /// Applies the primary ordering to the IQueryable query.
    /// </summary>
    /// <param name="query">The query to which the primary ordering will be applied.</param>
    /// <returns>The ordered query with the primary ordering applied.</returns>
    public IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query)
    {
        return _ascending
            ? query.OrderBy(_expression)
            : query.OrderByDescending(_expression);
    }
}