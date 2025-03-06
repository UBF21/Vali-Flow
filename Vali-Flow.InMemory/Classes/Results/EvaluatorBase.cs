using System.Numerics;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Classes.Options;
using Vali_Flow.InMemory.Interfaces.Evaluators.Read;
using Vali_Flow.InMemory.Interfaces.Evaluators.Write;

namespace Vali_Flow.InMemory.Classes.Results;

public abstract class EvaluatorBase<T, TProperty> : IInMemoryEvaluatorRead<T>, IInMemoryEvaluatorWrite<T> where T : class
{
    protected readonly ValiFlowEvaluator<T, TProperty> Evaluator;

    protected EvaluatorBase(IEnumerable<T>? initialData = null, Func<T, TProperty>? getId = null)
    {
        Evaluator = new ValiFlowEvaluator<T, TProperty>(initialData, getId: getId);
    }

    public void SetValiFlow(ValiFlow<T> valiFlow)
    {
        Evaluator.SetValiFlow(valiFlow);
    }

    public bool Evaluate(T entity)
    {
        return Evaluator.Evaluate(entity);
    }

    public bool EvaluateAny(IEnumerable<T> entities)
    {
      return Evaluator.EvaluateAny(entities);
    }

    public int EvaluateCount(IEnumerable<T> entities)
    {
        return Evaluator.EvaluateCount(entities);
    }

    public T? GetFirstFailed(IEnumerable<T> entities)
    {
       return Evaluator.GetFirstFailed(entities);
    }

    public T? GetFirst(IEnumerable<T> entities)
    {
       return Evaluator.GetFirst(entities);
    }

    public IEnumerable<T> EvaluateAllFailed<TKey>(IEnumerable<T>? entities, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
       return Evaluator.EvaluateAllFailed(entities, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateAll<TKey>(IEnumerable<T>? entities, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.EvaluateAll(entities, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluatePaged<TKey>(IEnumerable<T> entities, int page, int pageSize, Func<T, TKey>? orderBy = null,
        bool ascending = true, IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.EvaluatePaged(entities, page, pageSize, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateTop<TKey>(IEnumerable<T> entities, int count, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.EvaluateTop(entities, count, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateDistinct<TKey>(IEnumerable<T> entities, Func<T, TKey> selector, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.EvaluateDistinct(entities, selector, orderBy, ascending, thenBys);
    }

    public IEnumerable<T> EvaluateDuplicates<TKey>(IEnumerable<T> entities, Func<T, TKey> selector, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.EvaluateDuplicates(entities, selector, orderBy, ascending, thenBys);
    }

    public int GetFirstMatchIndex<TKey>(IEnumerable<T> entities, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.GetFirstMatchIndex(entities, orderBy, ascending, thenBys);
    }

    public int GetLastMatchIndex<TKey>(IEnumerable<T> entities, Func<T, TKey>? orderBy = null, bool ascending = true,
        IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.GetLastMatchIndex(entities, orderBy, ascending, thenBys);
    }

    public T? GetLastFailed<TKey>(IEnumerable<T> entities, Func<T, TKey>? orderBy = null, bool ascending = true, IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.GetLastFailed(entities, orderBy, ascending, thenBys);
    }

    public T? GetLast<TKey>(IEnumerable<T> entities, Func<T, TKey>? orderBy = null, bool ascending = true, IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null)
    {
        return Evaluator.GetLast(entities, orderBy, ascending, thenBys);
    }

    public TResult EvaluateMin<TResult>(IEnumerable<T> entities, Func<T, TResult> selector) where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateMin(entities, selector);
    }

    public TResult EvaluateMax<TResult>(IEnumerable<T> entities, Func<T, TResult> selector) where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateMax(entities, selector);
    }

    public decimal EvaluateAverage<TResult>(IEnumerable<T> entities, Func<T, TResult> selector) where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateAverage(entities, selector);
    }

    public TResult EvaluateSum<TResult>(IEnumerable<T> entities, Func<T, TResult> selector) where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateSum(entities, selector);
    }

    public TResult EvaluateAggregate<TResult>(IEnumerable<T> entities, Func<T, TResult> selector, Func<TResult, TResult, TResult> aggregator) where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateAggregate(entities, selector, aggregator);
    }

    public Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector) where TKey : notnull
    {
        return Evaluator.EvaluateGrouped(entities, keySelector);
    }

    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector) where TKey : notnull
    {
        return Evaluator.EvaluateCountByGroup(entities, keySelector);
    }

    public Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(IEnumerable<T> entities, Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateSumByGroup(entities, keySelector, selector);
    }

    public Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(IEnumerable<T> entities, Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateMinByGroup(entities, keySelector, selector);
    }

    public Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(IEnumerable<T> entities, Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateMaxByGroup(entities, keySelector, selector);
    }

    public Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(IEnumerable<T> entities, Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        return Evaluator.EvaluateAverageByGroup(entities, keySelector, selector);
    }

    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector) where TKey : notnull
    {
        return Evaluator.EvaluateDuplicatesByGroup(entities, keySelector);
    }

    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector) where TKey : notnull
    {
        return Evaluator.EvaluateUniquesByGroup(entities, keySelector);
    }

    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector, int count, Func<T, object>? orderBy = null,
        bool ascending = true) where TKey : notnull
    {
        return Evaluator.EvaluateTopByGroup(entities, keySelector, count, orderBy, ascending);
    }

    public void Add(T entity)
    {
        Evaluator.Add(entity);
    }

    public T? Update(T entity)
    {
        return Evaluator.Update(entity);
    }

    public bool Delete(T entity)
    {
        return Evaluator.Delete(entity);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        Evaluator.AddRange(entities);
    }

    public IEnumerable<T> UpdateRange(IEnumerable<T> entities)
    {
        return Evaluator.UpdateRange(entities);
    }

    public int DeleteRange(IEnumerable<T> entities)
    {
        return Evaluator.DeleteRange(entities);
    }

    public void SaveChanges()
    {
        Evaluator.SaveChanges();
    }
}