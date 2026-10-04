using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Models;

namespace Vali_Flow.Interfaces.Repository;

/// <summary>
/// High-level, mockable convenience wrapper over <see cref="Vali_Flow.Classes.Evaluators.ValiFlowEvaluator{T}"/>.
/// Every member delegates to the underlying evaluator — it does not reimplement any query or persistence logic.
/// </summary>
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
public interface IGenericRepository<T, TKey> where T : class where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Retrieves the entity whose key (as defined by the <c>idSelector</c> passed to the repository constructor)
    /// equals <paramref name="id"/>, or <c>null</c> if none matches.
    /// </summary>
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities matching <paramref name="filter"/> (or every entity when <paramref name="filter"/> is <c>null</c>).
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(ValiFlowQuery<T>? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of entities matching <paramref name="filter"/>, ordered by <paramref name="orderBy"/>.
    /// Ordering is required because deterministic pagination needs an ORDER BY.
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync<TOrderKey>(
        ValiFlowQuery<T>? filter,
        Expression<Func<T, TOrderKey>> orderBy,
        int page,
        int pageSize,
        bool ascending = true,
        CancellationToken cancellationToken = default
    ) where TOrderKey : notnull;

    /// <summary>Adds a single entity. See <see cref="Vali_Flow.Interfaces.Evaluators.Write.IEvaluatorWrite{T}.AddAsync"/>.</summary>
    Task<T> AddAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default);

    /// <summary>Updates a single entity. See <see cref="Vali_Flow.Interfaces.Evaluators.Write.IEvaluatorWrite{T}.UpdateAsync"/>.</summary>
    Task<T> UpdateAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default);

    /// <summary>Deletes a single entity. See <see cref="Vali_Flow.Interfaces.Evaluators.Write.IEvaluatorWrite{T}.DeleteAsync"/>.</summary>
    Task DeleteAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default);

    /// <summary>Persists any pending changes tracked by the underlying <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
