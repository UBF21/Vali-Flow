using System.Numerics;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;

namespace Vali_Flow.InMemory.Classes.Repositories;

public abstract class EvaluatorBase<T, TProperty> : IInMemoryEvaluatorRead<T>, IInMemoryEvaluatorWrite<T>
    where T : class
{
    protected readonly ValiFlowEvaluator<T, TProperty> Evaluator;

    protected EvaluatorBase(IEnumerable<T>? initialData = null, Func<T, TProperty>? getId = null)
    {
        Evaluator = new ValiFlowEvaluator<T, TProperty>(initialData, getId: getId);
    }

    public void SetValiFlow(ValiFlow<T> valiFlow) => Evaluator.SetValiFlow(valiFlow);

    public bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.Evaluate(entity, valiFlow, negateCondition);

    public bool EvaluateAny(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.EvaluateAny(entities, valiFlow, negateCondition);

    public int EvaluateCount(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.EvaluateCount(entities, valiFlow, negateCondition);

    public T? GetFirstFailed(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.GetFirstFailed(entities, valiFlow, negateCondition);

    public T? GetFirst(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.GetFirst(entities, valiFlow, negateCondition);

    public IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluateAllFailed(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);
    
    public IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluateAll(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);
    
    public IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T>? entities,
        int page,
        int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluatePaged(entities, page, pageSize, orderBy, ascending, thenBys, valiFlow,
        negateCondition);
    
    public IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T>? entities,
        int count,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluateTop(entities, count, orderBy, ascending, thenBys, valiFlow, negateCondition);
    
    public IEnumerable<T> EvaluateDistinct<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluateDistinct(entities, selector, orderBy, ascending, thenBys, valiFlow, negateCondition);
    
    public IEnumerable<T> EvaluateDuplicates<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> selector,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluateDuplicates(entities, selector, orderBy, ascending, thenBys, valiFlow, negateCondition);

    public int GetFirstMatchIndex<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.GetFirstMatchIndex(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);

    public int GetLastMatchIndex<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.GetLastMatchIndex(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);
    
    public T? GetLastFailed<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.GetLastFailed(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);

    public T? GetLast<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.GetLast(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);
    
    public TResult EvaluateMin<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
        => Evaluator.EvaluateMin(entities, selector, valiFlow, negateCondition);
    
    public TResult EvaluateMax<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
        => Evaluator.EvaluateMax(entities, selector, valiFlow, negateCondition);
    
    public decimal EvaluateAverage<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
        => Evaluator.EvaluateAverage(entities, selector, valiFlow, negateCondition);
    
    public TResult EvaluateSum<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
        => Evaluator.EvaluateSum(entities, selector, valiFlow, negateCondition);

    public TResult EvaluateAggregate<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>
        => Evaluator.EvaluateAggregate(entities, selector, aggregator, valiFlow, negateCondition);

    public Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
        => Evaluator.EvaluateGrouped(entities, keySelector, valiFlow, negateCondition);

    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
        => Evaluator.EvaluateCountByGroup(entities, keySelector, valiFlow, negateCondition);
    
    public Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
        => Evaluator.EvaluateSumByGroup(entities, keySelector, selector, valiFlow, negateCondition);

    public Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
        => Evaluator.EvaluateMinByGroup(entities, keySelector, selector, valiFlow, negateCondition);

    public Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
        => Evaluator.EvaluateMaxByGroup(entities, keySelector, selector, valiFlow, negateCondition);
    
    public Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull where TResult : INumber<TResult>
        => Evaluator.EvaluateAverageByGroup(entities, keySelector, selector, valiFlow, negateCondition);
    
    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
        => Evaluator.EvaluateDuplicatesByGroup(entities, keySelector, valiFlow, negateCondition);

    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
        => Evaluator.EvaluateUniquesByGroup(entities, keySelector, valiFlow, negateCondition);
    
    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, object>? orderBy = null,
        bool ascending = true,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
        => Evaluator.EvaluateTopByGroup(entities, keySelector, count, orderBy, ascending, valiFlow, negateCondition);

    public bool Add(T entity, IEnumerable<T>? entities) => Evaluator.Add(entity, entities);
    
    public T? Update(T entity, IEnumerable<T>? entities) => Evaluator.Update(entity, entities);
    
    public bool Delete(T entity, IEnumerable<T>? entities) => Evaluator.Delete(entity, entities);
    
    public void AddRange(IEnumerable<T> entitiesToAdd, IEnumerable<T>? entities) =>
        Evaluator.AddRange(entitiesToAdd, entities);
    
    public IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, IEnumerable<T>? entities = null) =>
        Evaluator.UpdateRange(entitiesToUpdate, entities);
    
    public int DeleteRange(IEnumerable<T> entitiesToDelete, IEnumerable<T>? entities = null) =>
        Evaluator.DeleteRange(entitiesToDelete, entities);
    
    public void SaveChanges(IEnumerable<T>? entities = null) => Evaluator.SaveChanges(entities);
}