using System.Linq.Expressions;
using System.Numerics;

namespace vali_flow.Interfaces.Evaluators;

//29 
public interface IDatabaseEvaluator<T>
{
    Task<bool> EvaluateAsync(T entity);

    Task<bool> EvaluateAnyAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<int> EvaluateCountAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<T?> GetFirstFailedAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<T?> GetFirstAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<IQueryable<T>> EvaluateAllFailedAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<IQueryable<T>> EvaluateAllAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<IQueryable<T>> EvaluatePagedAsync<TKey, TProperty>(
        IQueryable<T> query,
        int page,
        int pageSize,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<IQueryable<T>> EvaluateTopAsync<TKey, TProperty>(
        IQueryable<T> query,
        int count,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<IQueryable<T>> EvaluateDistinctAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey, TProperty>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> selector,
        int? page = null,
        int? pageSize = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        Expression<Func<T, TKey>>? thenBy = null,
        bool thenAscending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<int> GetFirstMatchIndexAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<int> GetLastMatchIndexAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<T?> GetLastFailedAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<T?> GetLastAsync<TProperty>(
        IQueryable<T> query,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null);

    Task<TResult> EvaluateMinAsync<TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult>;

    Task<TResult> EvaluateMaxAsync<TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult>;

    Task<TResult> EvaluateAverageAsync<TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult>;

    Task<TResult> EvaluateSumAsync<TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult>;

    Task<TResult> EvaluateAggregateAsync<TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult>;

    // Evalúa y agrupa los elementos que cumplen la condición
    Task<IEnumerable<Dictionary<TKey, T>>> EvaluateGroupedAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull;

    // Evalúa y agrupa los elementos con agregación de conteo
    IEnumerable<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull;

    // Evalúa y agrupa los elementos con suma de un campo seleccionado
    Task<IEnumerable<Dictionary<TKey, T>>> EvaluateSumByGroupAsync<TKey, TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el valor mínimo por grupo
    Task<IEnumerable<Dictionary<TKey, T>>> EvaluateMinByGroupAsync<TKey, TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el valor máximo por grupo
    Task<IEnumerable<Dictionary<TKey, T>>> EvaluateMaxByGroupAsync<TKey, TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el promedio de un campo por grupo
    Task<IEnumerable<Dictionary<TKey, T>>> EvaluateAverageByGroupAsync<TKey, TResult, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y devuelve los grupos que tienen más de un elemento (duplicados)
    Task<IEnumerable<Dictionary<TKey, T>>> EvaluateDuplicatesByGroupAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull;

    // Evalúa y devuelve los grupos que contienen solo un elemento (únicos)
    Task<IEnumerable<Dictionary<TKey, T>>> EvaluateUniquesByGroupAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull;

    // Evalúa y devuelve los N primeros elementos de cada grupo ordenados
    Task<IEnumerable<Dictionary<TKey, T>>> EvaluateTopByGroupAsync<TKey, TProperty>(
        IQueryable<T> query,
        Func<T, TKey> keySelector,
        int count,
        Func<T, object>? orderBy = null,
        bool ascending = true,
        IEnumerable<Expression<Func<T, TProperty>>>? includes = null
    ) where TKey : notnull;
}