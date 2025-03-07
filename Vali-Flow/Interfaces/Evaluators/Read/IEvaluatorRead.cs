using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Classes.Options;
using Vali_Flow.Classes.Results;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;

namespace Vali_Flow.Interfaces.Evaluators.Read;

public interface IEvaluatorRead<T>
{
    Task<bool> EvaluateAsync(ValiFlow<T> valiFlow, T entity);

    Task<bool> EvaluateAnyAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default);

    Task<int> EvaluateCountAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<T?> GetFirstFailedAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<T?> GetFirstAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<IQueryable<T>> EvaluateAllFailedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    )
        where TKey : notnull;

    Task<IQueryable<T>> EvaluateAllAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    Task<IQueryable<T>> EvaluatePagedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.Ten,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    )
        where TKey : notnull;

    Task<IQueryable<T>> EvaluateTopAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int count,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    Task<IQueryable<T>> EvaluateDistinctAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    Task<T?> GetLastFailedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    Task<T?> GetLastAsync<TProperty>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<TResult> EvaluateMinAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    Task<TResult> EvaluateMaxAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    Task<decimal> EvaluateAverageAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    Task<int> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, int>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<long> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, long>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<double> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, double>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<decimal> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, decimal>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<float> EvaluateSumAsync<TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, float>> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    );

    Task<TResult> EvaluateAggregateAsync<TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TResult>> selector,
        Func<TResult, TResult, TResult> aggregator,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult>;

    // Evalúa y agrupa los elementos que cumplen la condición
    Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Expression<Func<T, TKey>> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    // Evalúa y agrupa los elementos con agregación de conteo
    Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    // Evalúa y agrupa los elementos con suma de un campo seleccionado
    Task<Dictionary<TKey, TResult>> EvaluateSumByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el valor mínimo por grupo
    Task<Dictionary<TKey, TResult>> EvaluateMinByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el valor máximo por grupo
    Task<Dictionary<TKey, TResult>> EvaluateMaxByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el promedio de un campo por grupo
    Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y devuelve los grupos que tienen más de un elemento (duplicados)
    Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    // Evalúa y devuelve los grupos que contienen solo un elemento (únicos)
    Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    // Evalúa y devuelve los N primeros elementos de cada grupo ordenados
    Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey, TKey2, TProperty>(
        ValiFlow<T> valiFlow,
        Func<T, TKey> keySelector,
        int count,
        Expression<Func<T, TKey2>>? orderBy = null,
        bool ascending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull where TKey2 : notnull;

    Task<IQueryable<T>> EvaluateQuery<TProperty, TKey>(
        ValiFlow<T> valiFlow,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;

    Task<PaginatedBlockResult<T>> GetPaginatedBlockAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int blockSize = ConstantHelper.Thousand,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.OneHundred,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false,
        CancellationToken cancellationToken = default
    ) where TKey : notnull;

    Task<IQueryable<T>> GetPaginatedBlockQueryAsync<TKey, TProperty>(
        ValiFlow<T> valiFlow,
        int blockSize = ConstantHelper.Thousand,
        int page = ConstantHelper.One,
        int pageSize = ConstantHelper.OneHundred,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        IEnumerable<EfOrderThenBy<T, TKey>>? thenBys = null,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null,
        bool asNoTracking = true,
        bool asSplitQuery = false
    ) where TKey : notnull;
}