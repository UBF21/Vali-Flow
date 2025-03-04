using System.Numerics;
using vali_flow.Classes.Options;

namespace vali_flow.Interfaces.Evaluators.Read;

//29
public interface IInMemoryEvaluatorRead<T>
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
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T> entities,
        int page,
        int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T> entities,
        int count,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    IEnumerable<T> EvaluateDistinct<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    IEnumerable<T> EvaluateDuplicates<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    int GetFirstMatchIndex<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    int GetLastMatchIndex<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    T? GetLastFailed<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    T? GetLast<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<ThenByInMemoryExpression<T, TKey>>? thenBys = null
    );

    TResult EvaluateMin<TResult>(IEnumerable<T> entities, Func<T, TResult> selector) where TResult : INumber<TResult>;

    TResult EvaluateMax<TResult>(IEnumerable<T> entities, Func<T, TResult> selector) where TResult : INumber<TResult>;

    decimal EvaluateAverage<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>;

    TResult EvaluateSum<TResult>(IEnumerable<T> entities, Func<T, TResult> selector) where TResult : INumber<TResult>;

    TResult EvaluateAggregate<TResult>(
        IEnumerable<T> entities,
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator
    ) where TResult : INumber<TResult>;

    // Evalúa y agrupa los elementos que cumplen la condición
    Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector
    ) where TKey : notnull;


    // Evalúa y agrupa los elementos con agregación de conteo
    Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector
    ) where TKey : notnull;

    // Evalúa y agrupa los elementos con suma de un campo seleccionado
    Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el valor mínimo por grupo
    Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el valor máximo por grupo
    Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y agrupa los elementos con el promedio de un campo por grupo
    Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector
    ) where TResult : INumber<TResult> where TKey : notnull;

    // Evalúa y devuelve los grupos que tienen más de un elemento (duplicados)
    Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector
    ) where TKey : notnull;

    // Evalúa y devuelve los grupos que contienen solo un elemento (únicos)
    Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector
    ) where TKey : notnull;

    // Evalúa y devuelve los N primeros elementos de cada grupo ordenados
    Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, object>? orderBy = null,
        bool ascending = true) where TKey : notnull;
}