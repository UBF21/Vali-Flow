namespace Vali_Flow.Interfaces.Options;

/// <summary>
/// Defines a primary ordering expression (OrderBy) to be applied to an IQueryable query.
/// </summary>
/// <typeparam name="T">The type of the entity being ordered.</typeparam>
public interface IEfOrderBy<T>
{
    /// <summary>
    /// Applies the primary ordering to an IQueryable query.
    /// </summary>
    /// <param name="query">The query to which the primary ordering will be applied.</param>
    /// <returns>The ordered query with the primary ordering applied.</returns>
    IOrderedQueryable<T> ApplyOrderBy(IQueryable<T> query);
}