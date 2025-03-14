namespace Vali_Flow.Interfaces.Specification;

/// <summary>
/// Represents an inclusion expression that can be applied to an IQueryable to include related properties.
/// </summary>
/// <typeparam name="T">The type of entity to which the inclusion is applied.</typeparam>
public interface IIncludeExpression<T>
{
    /// <summary>
    /// Applies the inclusion expression to an IQueryable to include related properties.
    /// </summary>
    /// <param name="query">The IQueryable query to which the inclusion will be applied.</param>
    /// <returns>The updated IQueryable query with the inclusions applied.</returns>
    IQueryable<T> ApplyInclude(IQueryable<T> query);
}