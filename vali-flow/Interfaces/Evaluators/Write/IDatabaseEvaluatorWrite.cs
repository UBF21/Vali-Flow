using System.Linq.Expressions;

namespace vali_flow.Interfaces.Evaluators.Write;

public interface IDatabaseEvaluatorWrite<T>
{
    /// <summary>
    /// Inserta una entidad en la base de datos de forma asíncrona.
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta múltiples entidades en la base de datos de forma asíncrona.
    /// </summary>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una entidad existente en la base de datos de forma asíncrona.
    /// </summary>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Actualiza múltiples entidades existentes en la base de datos de forma asíncrona.
    /// </summary>
    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Elimina una entidad de la base de datos de forma asíncrona.
    /// </summary>
    Task DeleteAsync(T entity);

    /// <summary>
    /// Elimina múltiples entidades de la base de datos de forma asíncrona.
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Guarda todos los cambios pendientes en la base de datos de forma asíncrona.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> UpsertAsync(
        T entity,
        Expression<Func<T, bool>> matchCondition,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
        IEnumerable<T> entities,
        Func<T, TProperty> keySelector,
        CancellationToken cancellationToken = default
    ) where TProperty : notnull;

    Task DeleteByConditionAsync(Expression<Func<T, bool>> condition, CancellationToken cancellationToken = default);

    Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default);
}