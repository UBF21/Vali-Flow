using System.Linq.Expressions;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.Classes.Evaluators;

public sealed partial class ValiFlowEvaluator<T>
{
    async Task<bool> IQueryReader<T>.EvaluateAnyAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();
        if (filter != null) query = query.Where(filter.Build());
        return await query.AnyAsync(cancellationToken);
    }

    async Task<int> IQueryReader<T>.EvaluateCountAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();
        if (filter != null) query = query.Where(filter.Build());
        return await query.CountAsync(cancellationToken);
    }

    async Task<T?> IQueryReader<T>.EvaluateGetFirstAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();
        if (filter != null) query = query.Where(filter.Build());
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    async Task<T?> IQueryReader<T>.EvaluateGetLastAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
    {
        return await ExecuteWithExceptionHandlingAsync(async () =>
        {
            var pk = _dbContext.Model.FindEntityType(typeof(T))?.FindPrimaryKey();
            if (pk == null)
                throw new InvalidOperationException($"Entity '{typeof(T).Name}' has no primary key mapped.");
            if (pk.Properties.Count > 1)
                throw new NotSupportedException(
                    $"EvaluateGetLastAsync does not support composite primary keys for '{typeof(T).Name}'. Use EvaluateGetLastAsync with an explicit OrderBy specification instead.");

            IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();
            if (filter != null) query = query.Where(filter.Build());
            // Use OrderByDescending + FirstOrDefaultAsync to avoid loading the full table into memory
            return await query.OrderByDescending(e => EF.Property<object>(e, GetPrimaryKeyName())).FirstOrDefaultAsync(cancellationToken);
        }, nameof(IQueryReader<T>.EvaluateGetLastAsync));
    }

    async Task<TResult> IQueryAggregator<T>.EvaluateMinAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();
        if (filter != null) query = query.Where(filter.Build());
        if (!await query.AnyAsync(cancellationToken))
            return default(TResult)!;
        return await query.MinAsync(selector, cancellationToken);
    }

    async Task<TResult> IQueryAggregator<T>.EvaluateMaxAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();
        if (filter != null) query = query.Where(filter.Build());
        if (!await query.AnyAsync(cancellationToken))
            return default(TResult)!;
        return await query.MaxAsync(selector, cancellationToken);
    }

    async Task<decimal> IQueryAggregator<T>.EvaluateAverageAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();
        if (filter != null) query = query.Where(filter.Build());
        if (!await query.AnyAsync(cancellationToken)) return 0m;

        // Delegate to EF Core for supported types to avoid materializing the full table.
        if (selector is Expression<Func<T, decimal>> decSel)
            return await query.AverageAsync(decSel, cancellationToken);
        if (selector is Expression<Func<T, double>> dblSel)
            return (decimal)await query.AverageAsync(dblSel, cancellationToken);
        if (selector is Expression<Func<T, float>> fltSel)
            return (decimal)await query.AverageAsync(fltSel, cancellationToken);
        if (selector is Expression<Func<T, long>> lngSel)
            return (decimal)await query.AverageAsync(lngSel, cancellationToken);
        if (selector is Expression<Func<T, int>> intSel)
            return (decimal)await query.AverageAsync(intSel, cancellationToken);

        // Fallback for exotic numeric types — materialize only when no EF Core overload exists.
        var values = await query.Select(selector).ToListAsync(cancellationToken);
        if (values.Count == 0) return 0m;
        decimal total = values.Aggregate(0m, (acc, x) => acc + decimal.CreateChecked(x));
        return total / values.Count;
    }

    async Task<TResult> IQueryAggregator<T>.EvaluateSumAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
    {
        IQueryable<T> query = _dbContext.Set<T>().AsNoTracking();
        if (filter != null) query = query.Where(filter.Build());

        // Delegate to EF Core for supported types to avoid materializing the full table.
        if (selector is Expression<Func<T, decimal>> decSel)
            return (TResult)(object)await query.SumAsync(decSel, cancellationToken);
        if (selector is Expression<Func<T, double>> dblSel)
            return (TResult)(object)await query.SumAsync(dblSel, cancellationToken);
        if (selector is Expression<Func<T, float>> fltSel)
            return (TResult)(object)await query.SumAsync(fltSel, cancellationToken);
        if (selector is Expression<Func<T, long>> lngSel)
            return (TResult)(object)await query.SumAsync(lngSel, cancellationToken);
        if (selector is Expression<Func<T, int>> intSel)
            return (TResult)(object)await query.SumAsync(intSel, cancellationToken);

        // Fallback for exotic numeric types — materialize only when no EF Core overload exists.
        var values = await query.Select(selector).ToListAsync(cancellationToken);
        if (values.Count == 0) return TResult.Zero;
        return values.Aggregate(TResult.Zero, (acc, v) => acc + v);
    }

    private volatile string? _cachedPrimaryKeyName;

    private string GetPrimaryKeyName()
    {
        if (_cachedPrimaryKeyName != null) return _cachedPrimaryKeyName;
        var entityType = _dbContext.Model.FindEntityType(typeof(T))
            ?? throw new InvalidOperationException($"Entity type {typeof(T).Name} not found in model.");
        var pkName = entityType.FindPrimaryKey()?.Properties[0].Name
            ?? throw new InvalidOperationException($"No primary key found for {typeof(T).Name}.");
        Interlocked.CompareExchange(ref _cachedPrimaryKeyName, pkName, null);
        return _cachedPrimaryKeyName!;
    }
}
