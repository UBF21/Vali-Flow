using System.Linq.Expressions;
using System.Numerics;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;
using Vali_Flow.InMemory.Models;

namespace Vali_Flow.InMemory.Classes.Repositories;

public abstract class EvaluatorBase<T, TProperty> : IInMemoryEvaluatorRead<T>, IInMemoryEvaluatorWrite<T>
    where T : class
    where TProperty : notnull
{
    protected readonly ValiFlowEvaluator<T, TProperty> Evaluator;

    protected EvaluatorBase(
        IEnumerable<T>? initialData = null,
        ValiFlow<T>? valiFlow = null,
        Func<T, TProperty>? getId = null)
    {
        Evaluator = new ValiFlowEvaluator<T, TProperty>(initialData, valiFlow, getId);
    }

    protected EvaluatorBase(ValiFlowEvaluator<T, TProperty> evaluator)
    {
        Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public void SetValiFlow(ValiFlow<T> valiFlow) => Evaluator.SetValiFlow(valiFlow);

    public bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.Evaluate(entity, valiFlow, negateCondition);

    public bool EvaluateAny(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.EvaluateAny(entities, valiFlow, negateCondition);

    public int EvaluateCount(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.EvaluateCount(entities, valiFlow, negateCondition);

    public T? GetFirstFailed(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null) =>
        Evaluator.GetFirstFailed(entities, valiFlow);

    public T? GetFirst(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false) =>
        Evaluator.GetFirst(entities, valiFlow, negateCondition);

    public IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null
    ) => Evaluator.EvaluateAllFailed(entities, orderBy, ascending, thenBys, valiFlow);

    public IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluateAll(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);
    
    public IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T>? entities = null,
        int page = 1,
        int pageSize = 10,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluatePaged(entities, page, pageSize, orderBy, ascending, thenBys, valiFlow,
        negateCondition);
    
    public IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T>? entities = null,
        int count = 10,
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
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.GetFirstMatchIndex(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);

    public int GetLastMatchIndex<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.GetLastMatchIndex(entities, orderBy, ascending, thenBys, valiFlow, negateCondition);
    
    public T? GetLastFailed<TKey>(
        IEnumerable<T>? entities = null,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null
    ) => Evaluator.GetLastFailed(entities, orderBy, ascending, thenBys, valiFlow);

    public T? GetLast<TKey>(
        IEnumerable<T>? entities = null,
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
    
    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey, TOrderKey>(
        IEnumerable<T>? entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, TOrderKey>? orderBy = null,
        bool ascending = true,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TKey : notnull
        => Evaluator.EvaluateTopByGroup(entities, keySelector, count, orderBy, ascending, valiFlow, negateCondition);

    public PagedResult<T> EvaluatePagedResult<TKey>(
        IEnumerable<T>? entities,
        int page,
        int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) => Evaluator.EvaluatePagedResult(entities, page, pageSize, orderBy, ascending, thenBys, valiFlow, negateCondition);

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

    public T Upsert(T entity, IEnumerable<T>? entities = null) => Evaluator.Upsert(entity, entities);

    public IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, IEnumerable<T>? entities = null)
        => Evaluator.UpsertRange(entitiesToUpsert, entities);

    public int DeleteByCondition(Func<T, bool> predicate, IEnumerable<T>? entities = null)
        => Evaluator.DeleteByCondition(predicate, entities);

    #region IQueryReader<T> + IQueryAggregator<T> — provider-agnostic methods

    Task<bool> IQueryReader<T>.EvaluateAnyAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => ((IQueryReader<T>)Evaluator).EvaluateAnyAsync(filter, cancellationToken);

    Task<int> IQueryReader<T>.EvaluateCountAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => ((IQueryReader<T>)Evaluator).EvaluateCountAsync(filter, cancellationToken);

    Task<T?> IQueryReader<T>.EvaluateGetFirstAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => ((IQueryReader<T>)Evaluator).EvaluateGetFirstAsync(filter, cancellationToken);

    Task<T?> IQueryReader<T>.EvaluateGetLastAsync(ValiFlow<T>? filter, CancellationToken cancellationToken)
        => ((IQueryReader<T>)Evaluator).EvaluateGetLastAsync(filter, cancellationToken);

    Task<TResult> IQueryAggregator<T>.EvaluateMinAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => ((IQueryAggregator<T>)Evaluator).EvaluateMinAsync(selector, filter, cancellationToken);

    Task<TResult> IQueryAggregator<T>.EvaluateMaxAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => ((IQueryAggregator<T>)Evaluator).EvaluateMaxAsync(selector, filter, cancellationToken);

    Task<decimal> IQueryAggregator<T>.EvaluateAverageAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => ((IQueryAggregator<T>)Evaluator).EvaluateAverageAsync(selector, filter, cancellationToken);

    Task<TResult> IQueryAggregator<T>.EvaluateSumAsync<TResult>(
        Expression<Func<T, TResult>> selector, ValiFlow<T>? filter, CancellationToken cancellationToken)
        => ((IQueryAggregator<T>)Evaluator).EvaluateSumAsync(selector, filter, cancellationToken);

    #endregion
}