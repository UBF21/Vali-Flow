namespace Vali_Flow.Interfaces.Options;

/// <summary>
/// Defines a secondary ordering expression (ThenBy) to be applied to an IQueryable query.
/// </summary>
/// <typeparam name="T">The type of the entity being ordered.</typeparam>
public interface IEfOrderThenBy<T>
{
    /// <summary>
    /// Applies the secondary ordering to an ordered query.
    /// </summary>
    /// <param name="query">The ordered query to which the secondary ordering will be applied.</param>
    /// <returns>The updated ordered query with the secondary ordering applied.</returns>
    IOrderedQueryable<T> ApplyThenBy(IOrderedQueryable<T> query);
}