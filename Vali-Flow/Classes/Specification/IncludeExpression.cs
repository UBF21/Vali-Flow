using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Classes.Specification;

/// <summary>
/// Represents an inclusion expression that can be applied to an IQueryable to include related properties of a specific type.
/// </summary>
/// <typeparam name="T">The type of entity to which the inclusion applies.</typeparam>
/// <typeparam name="TProperty">The type of the related property to include.</typeparam>
public class IncludeExpression<T, TProperty> : IIncludeExpression<T> where T : class
{
    /// <summary>
    /// Gets the expression that defines the related property to include.
    /// </summary>
    private Expression<Func<T, TProperty>> Expression { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeExpression{T, TProperty}"/> class with an inclusion expression.
    /// </summary>
    /// <param name="expression">The expression that defines the related property to include. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="expression"/> parameter is null.</exception>
    public IncludeExpression(Expression<Func<T, TProperty>> expression)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <summary>
    /// Applies the inclusion expression to an IQueryable to include the related property.
    /// </summary>
    /// <param name="query">The IQueryable query to which the inclusion will be applied.</param>
    /// <returns>The updated IQueryable query with the inclusion applied.</returns>
    public IQueryable<T> ApplyInclude(IQueryable<T> query)
    {
        return query.Include(Expression);
    }
}