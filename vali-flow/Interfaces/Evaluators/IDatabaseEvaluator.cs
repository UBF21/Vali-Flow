using System.Linq.Expressions;

namespace vali_flow.Interfaces.Evaluators;

//29 
public interface IDatabaseEvaluator<T>
{
    Task<bool> EvaluateAsync(T entity);
    Task<bool> EvaluateAnyAsync(
        IQueryable<T> entities,
        params Expression<Func<T, object>>[] includes);
    Task<int> EvaluateCountAsync(
        IQueryable<T> entities,
        params Expression<Func<T, object>>[] includes);
    Task<T?> GetFirstFailedAsync(
        IQueryable<T> entities,
        params Expression<Func<T, object>>[] includes);
    Task<T?> GetFirstAsync(
        IQueryable<T> entities,
        params Expression<Func<T, object>>[] includes);

    Task<IQueryable<T>> EvaluateAllFailedAsync<TKey>(
        IQueryable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true,
        params Expression<Func<T, object>>[] includes);

    Task<IQueryable<T>> EvaluateAllAsync<TKey>(
        IQueryable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true,
        params Expression<Func<T, object>>[] includes);

    Task<IQueryable<T>> EvaluatePagedAsync<TKey>(
        IQueryable<T> entities, 
        int page, 
        int pageSize, 
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true,
        params Expression<Func<T, object>>[] includes);

    Task<IQueryable<T>> EvaluateTopAsync<TKey>(
        IQueryable<T> entities, 
        int count, 
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true,
        params Expression<Func<T, object>>[] includes);

    Task<IQueryable<T>> EvaluateDistinctAsync<TKey>(
        IQueryable<T> entities, 
        Func<T, TKey> selector,
        params Expression<Func<T, object>>[] includes);

    Task<IQueryable<T>> EvaluateDuplicatesAsync<TKey>(
        IQueryable<T> entities,
        Func<T, TKey> selector,
        params Expression<Func<T, object>>[] includes);
    
    Task<int> GetFirstMatchIndexAsync(
        IQueryable<T> entities,
        params Expression<Func<T, object>>[] includes);
    
    Task<int> GetLastMatchIndexAsync(
        IQueryable<T> entities,
        params Expression<Func<T, object>>[] includes);
    
    Task<T?> GetLastFailedAsync(
        IQueryable<T> entities,
        params Expression<Func<T, object>>[] includes);
    
    Task<T?> GetLastAsync(
        IQueryable<T> entities,
        params Expression<Func<T, object>>[] includes);
    
    Task<TResult> EvaluateMinAsync<TResult>(
        IQueryable<T> entities, 
        Func<T, TResult> selector,
        params Expression<Func<T, object>>[] includes);
    
    Task<TResult> EvaluateMaxAsync<TResult>(
        IQueryable<T> entities, 
        Func<T, TResult> selector,
        params Expression<Func<T, object>>[] includes);
    
    Task<TResult> EvaluateAverageAsync<TResult>(
        IQueryable<T> entities, 
        Func<T, TResult> selector,
        params Expression<Func<T, object>>[] includes);
    
    Task<TResult> EvaluateSumAsync<TResult>(
        IQueryable<T> entities, 
        Func<T, TResult> selector,
        params Expression<Func<T, object>>[] includes);

    Task<TResult> EvaluateAggregateAsync<TResult>(
        IQueryable<T> entities, 
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y agrupa los elementos que cumplen la condición
    Task<IEnumerable<IGrouping<TKey, T>>> EvaluateGroupedAsync<TKey>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y agrupa los elementos con agregación de conteo
    IEnumerable<IGrouping<TKey, int>> EvaluateCountByGroupAsync<TKey>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y agrupa los elementos con suma de un campo seleccionado
    Task<IEnumerable<IGrouping<TKey, T>>>EvaluateSumByGroupAsync<TKey, TResult>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y agrupa los elementos con el valor mínimo por grupo
    Task<IEnumerable<IGrouping<TKey, T>>> EvaluateMinByGroupAsync<TKey, TResult>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y agrupa los elementos con el valor máximo por grupo
    Task<IEnumerable<IGrouping<TKey, T>>>EvaluateMaxByGroupAsync<TKey, TResult>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y agrupa los elementos con el promedio de un campo por grupo
    Task<IEnumerable<IGrouping<TKey, T>>> EvaluateAverageByGroupAsync<TKey, TResult>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y devuelve los grupos que tienen más de un elemento (duplicados)
    Task<IEnumerable<IGrouping<TKey, T>>> EvaluateDuplicatesByGroupAsync<TKey>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y devuelve los grupos que contienen solo un elemento (únicos)
    Task<IEnumerable<IGrouping<TKey, T>>> EvaluateUniquesByGroupAsync<TKey>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector,
        params Expression<Func<T, object>>[] includes);
    
    // Evalúa y devuelve los N primeros elementos de cada grupo ordenados
    Task<IEnumerable<IGrouping<TKey, T>>> EvaluateTopByGroupAsync<TKey>(
        IQueryable<T> entities, 
        Func<T, TKey> keySelector, 
        int count, 
        Func<T, object>? orderBy = null, 
        bool ascending = true,
        params Expression<Func<T, object>>[] includes);

}