using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Repository;
using Vali_Flow.Models;

namespace Vali_Flow.Classes.Repository;

/// <summary>
/// Convenience repository that wraps a <see cref="ValiFlowEvaluator{T}"/>. It does not reimplement any
/// query/persistence logic — every member is a thin delegation to the evaluator's existing read/write surface.
/// </summary>
/// <typeparam name="T">The entity type. Must be a reference type.</typeparam>
/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
public sealed class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class where TKey : IEquatable<TKey>
{
    private readonly ValiFlowEvaluator<T> _evaluator;
    private readonly Expression<Func<T, TKey>> _idSelector;

    /// <summary>
    /// Initializes a new instance of <see cref="GenericRepository{T,TKey}"/>.
    /// </summary>
    /// <param name="evaluator">The underlying evaluator this repository delegates to.</param>
    /// <param name="idSelector">
    /// Expression identifying the entity's primary key (e.g. <c>x => x.Id</c>). <see cref="ValiFlowEvaluator{T}"/>
    /// has no built-in concept of a primary key — it only works with specifications/filters — so the repository
    /// needs this to build an <c>EqualTo</c> filter for <see cref="GetByIdAsync"/>.
    /// </param>
    public GenericRepository(ValiFlowEvaluator<T> evaluator, Expression<Func<T, TKey>> idSelector)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _idSelector = idSelector ?? throw new ArgumentNullException(nameof(idSelector));
    }

    /// <inheritdoc />
    public Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        ValiFlowQuery<T> filter = new ValiFlowQuery<T>().EqualTo(_idSelector, id);
        BasicSpecification<T> spec = new(filter);
        return _evaluator.EvaluateGetFirstAsync(spec, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetAllAsync(ValiFlowQuery<T>? filter = null, CancellationToken cancellationToken = default)
    {
        QuerySpecification<T> spec = filter is null ? new QuerySpecification<T>() : new QuerySpecification<T>(filter);
        IQueryable<T> query = await _evaluator.EvaluateQueryAsync(spec);
        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<T>> GetPagedAsync<TOrderKey>(
        ValiFlowQuery<T>? filter,
        Expression<Func<T, TOrderKey>> orderBy,
        int page,
        int pageSize,
        bool ascending = true,
        CancellationToken cancellationToken = default
    ) where TOrderKey : notnull
    {
        QuerySpecification<T> spec = filter is null ? new QuerySpecification<T>() : new QuerySpecification<T>(filter);
        spec.WithOrderBy(orderBy, ascending).WithPagination(page, pageSize);
        return _evaluator.EvaluatePagedResultAsync(spec, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T> AddAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
        => _evaluator.AddAsync(entity, saveChanges, cancellationToken);

    /// <inheritdoc />
    public Task<T> UpdateAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
        => _evaluator.UpdateAsync(entity, saveChanges, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
        => _evaluator.DeleteAsync(entity, saveChanges, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _evaluator.SaveChangesAsync(cancellationToken);
}
