namespace vali_flow.Interfaces.Evaluators;

//29
public interface IInMemoryEvaluator<T>
{
    bool Evaluate(T entity);
    bool EvaluateAny(IEnumerable<T> entities);
    int EvaluateCount(IEnumerable<T> entities);
    T? GetFirstFailed(IEnumerable<T> entities);
    T? GetFirst(IEnumerable<T> entities);

    IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true);

    IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true);

    IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T> entities,
        int page,
        int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true);

    IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T> entities,
        int count,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true);

    IEnumerable<T> EvaluateDistinct<TKey>(IEnumerable<T> entities, Func<T, TKey> selector);

    IEnumerable<T> EvaluateDuplicates<TKey>(IEnumerable<T> entities, Func<T, TKey> selector);

    int GetFirstMatchIndex(IEnumerable<T> entities);

    int GetLastMatchIndex(IEnumerable<T> entities);

    T? GetLastFailed(IEnumerable<T> entities);

    T? GetLast(IEnumerable<T> entities);

    TResult EvaluateMin<TResult>(IEnumerable<T> entities, Func<T, TResult> selector);

    TResult EvaluateMax<TResult>(IEnumerable<T> entities, Func<T, TResult> selector);

    TResult EvaluateAverage<TResult>(IEnumerable<T> entities, Func<T, TResult> selector);

    TResult EvaluateSum<TResult>(IEnumerable<T> entities, Func<T, TResult> selector);

    TResult EvaluateAggregate<TResult>(IEnumerable<T> entities, Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator);

    // Evalúa y agrupa los elementos que cumplen la condición
    Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector) where TKey : notnull;


    // Evalúa y agrupa los elementos con agregación de conteo
    Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector) where TKey : notnull;

    // Evalúa y agrupa los elementos con suma de un campo seleccionado
    Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector)
        where TResult : struct where TKey : notnull;

    // Evalúa y agrupa los elementos con el valor mínimo por grupo
    Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector)
        where TResult : struct, IComparable<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el valor máximo por grupo
    Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector)
        where TResult : struct, IComparable<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el promedio de un campo por grupo
    Dictionary<TKey, double> EvaluateAverageByGroup<TKey, TResult>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector)
        where TResult : struct where TKey : notnull;

    // Evalúa y devuelve los grupos que tienen más de un elemento (duplicados)
    Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector) where TKey : notnull;

    // Evalúa y devuelve los grupos que contienen solo un elemento (únicos)
    Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector) where TKey : notnull;

    // Evalúa y devuelve los N primeros elementos de cada grupo ordenados
    Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector, 
        int count, 
        Func<T, object>? orderBy = null, 
        bool ascending = true) where TKey : notnull;
}