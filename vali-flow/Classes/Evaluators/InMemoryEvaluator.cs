using System.Numerics;
using vali_flow.Classes.Base;
using vali_flow.Interfaces.Evaluators;
using vali_flow.Utils;

namespace vali_flow.Classes.Evaluators;

public class InMemoryEvaluator<TBuilder, T> : IInMemoryEvaluator<T>
    where TBuilder : BaseExpression<TBuilder, T>, IInMemoryEvaluator<T>, new()

{
    private readonly BaseExpression<TBuilder, T> _builder;

    public InMemoryEvaluator(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public bool Evaluate(T entity)
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            return condition(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public bool EvaluateAny(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            return entities.Any(condition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public int EvaluateCount(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            return entities.Count(condition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public T? GetFirstFailed(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> condition = _builder.BuildNegated().Compile();
            return entities.FirstOrDefault(condition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public T? GetFirst(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            return entities.FirstOrDefault(condition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true)
    {
        List<T> enumerable = entities.ToList();

        try
        {
            Func<T, bool> condition = _builder.BuildNegated().Compile();

            List<T> failedEntities = enumerable.Where(condition).ToList();
            IOrderedEnumerable<T> orderedEntities = ApplyOrdering(failedEntities, orderBy, ascending, thenBy, thenAscending);

            return orderedEntities;

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true)
    {
        List<T> enumerable = entities.ToList();

        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            List<T> passedEntities = enumerable.Where(condition).ToList();
            
            IOrderedEnumerable<T> orderedEntities = ApplyOrdering(passedEntities, orderBy, ascending, thenBy, thenAscending);

            return orderedEntities;

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T> entities,
        int page,
        int pageSize,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true)
    {
        List<T> enumerable = entities.ToList();
        
        ValidationHelper.ValidatePageZero(page);
        ValidationHelper.ValidatePageSizeZero(pageSize);

        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            IEnumerable<T> filteredEntities = enumerable.Where(condition);
            IOrderedEnumerable<T> orderedEntities = ApplyOrdering(filteredEntities, orderBy, ascending, thenBy, thenAscending);

            IEnumerable<T> pagedEntities = orderedEntities
                .Skip((page - ConstantsHelper.One) * pageSize)
                .Take(pageSize);

            return pagedEntities;

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T> entities,
        int count,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true)
    {
        List<T> enumerable = entities.ToList();

        ValidationHelper.ValidateCountZero(count);

        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            IEnumerable<T> filteredEntities = enumerable.Where(condition);
            IOrderedEnumerable<T> orderedEntities =
                ApplyOrdering(filteredEntities, orderBy, ascending, thenBy, thenAscending);

            return orderedEntities.Take(count);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public IEnumerable<T> EvaluateDistinct<TKey>(IEnumerable<T> entities, Func<T, TKey> selector)
    {
        List<T> enumerable = entities.ToList();

        ValidationHelper.ValidateEntitiesNotNull(enumerable);
        ValidationHelper.ValidateSelectorNotNull(selector);

        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            IEnumerable<T> filteredEntities = enumerable.Where(condition);

            return filteredEntities
                .GroupBy(selector)
                .Select(group => group.First());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public IEnumerable<T> EvaluateDuplicates<TKey>(IEnumerable<T> entities, Func<T, TKey> selector)
    {
        List<T> enumerable = entities.ToList();

        ValidationHelper.ValidateEntitiesNotNull(enumerable);
        ValidationHelper.ValidateSelectorNotNull(selector);

        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            return enumerable
                .Where(condition)
                .GroupBy(selector)
                .Where(group => group.Count() > ConstantsHelper.One)
                .SelectMany(group => group);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public int GetFirstMatchIndex(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            int index = ConstantsHelper.ZeroInt;
            foreach (T entity in entities)
            {
                if (condition(entity)) return index;
                index++;
            }

            return -ConstantsHelper.One;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public int GetLastMatchIndex(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            int index = ConstantsHelper.ZeroInt;
            int lastIndex = -ConstantsHelper.One;
            foreach (T entity in entities)
            {
                if (condition(entity)) lastIndex = index;
                index++;
            }

            return lastIndex;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public T? GetLastFailed(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> condition = _builder.BuildNegated().Compile();
            return entities.LastOrDefault(condition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public T? GetLast(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> condition = _builder.BuildNegated().Compile();
            return entities.LastOrDefault(condition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public TResult EvaluateMin<TResult>(
        IEnumerable<T> entities, 
        Func<T, TResult> selector
        ) where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(condition)
                .Select(selector)
                .ToList();

            return projectedValues.Any() ? projectedValues.Min() ?? TResult.Zero : TResult.Zero;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public TResult EvaluateMax<TResult>(
        IEnumerable<T> entities, 
        Func<T, TResult> selector
        ) where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(condition)
                .Select(selector)
                .ToList();

            return projectedValues.Any() ? projectedValues.Max() ?? TResult.Zero : TResult.Zero;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public decimal EvaluateAverage<TResult>(
        IEnumerable<T> entities, 
        Func<T, TResult> selector
        ) where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(condition)
                .Select(selector)
                .ToList();

            decimal averageValue = projectedValues.Any()
                ? projectedValues.Average(x => Convert.ToDecimal(x))
                : ConstantsHelper.ZeroDecimal;

            return averageValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public TResult EvaluateSum<TResult>(
        IEnumerable<T> entities, 
        Func<T, TResult> selector
        ) where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(condition)
                .Select(selector)
                .ToList();

            TResult sumValue = projectedValues.Any()
                ? projectedValues.Aggregate(TResult.Zero, (acc, x) => acc + x)
                : TResult.Zero;

            return sumValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public TResult EvaluateAggregate<TResult>(
        IEnumerable<T> entities,
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator)
        where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(condition)
                .Select(selector)
                .ToList();

            TResult result = projectedValues.Any() ? projectedValues.Aggregate(aggregator) : TResult.Zero;

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector
    ) where TKey : notnull
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Aggregate(TResult.Zero, (acc, x) => acc + x)
                );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector)
        where TKey : notnull where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .ToDictionary(g =>
                        g.Key,
                    g => g.Select(selector).Min() ?? TResult.Zero);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector)
        where TKey : notnull where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .ToDictionary(g =>
                        g.Key,
                    g => g.Select(selector).Max() ?? TResult.Zero);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector
    ) where TKey : notnull where TResult : INumber<TResult>

    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            
            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Average(e => Convert.ToDecimal(selector(e))));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector)
        where TKey : notnull
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            
            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .Where(g => g.Count() > ConstantsHelper.One)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector)
        where TKey : notnull
    {
        try
        {
            Func<T, bool> condition = _builder.Build().Compile();
            
            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .Where(g => g.Count() == ConstantsHelper.One)
                .ToDictionary(g => g.Key, g => g.First());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, object>? orderBy = null,
        bool ascending = true) where TKey : notnull
    {
        ValidationHelper.ValidateCountZero(count);

        try
        {
            Func<T, bool> condition = _builder.Build().Compile();

            return entities
                .Where(condition)
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g =>
                {
                    IEnumerable<T> group = g;
                    if (orderBy != null)
                    {
                        group = ascending ? group.OrderBy(orderBy) : group.OrderByDescending(orderBy);
                    }

                    return group.Take(count).ToList();
                });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error in evaluating the condition for {UtilHelper.GetCurrentMethodName()}.", ex);
        }
    }

    #region Private

    /// <summary>
    /// Applies ordering to a sequence of entities based on provided criteria.
    /// </summary>
    /// <typeparam name="T">The type of the entities in the sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    /// <param name="entities">The sequence of entities to order.</param>
    /// <param name="orderBy">The function to use for the initial ordering (can be null). If null, a default ordering by the entity itself is used.</param>
    /// <param name="ascending">A boolean indicating whether the initial ordering should be ascending.</param>
    /// <param name="thenBy">An optional function to use for secondary ordering (can be null).</param>
    /// <param name="thenAscending">A boolean indicating whether the secondary ordering should be ascending.</param>
    /// <returns>An ordered sequence of entities.</returns>
    private IOrderedEnumerable<T> ApplyOrdering<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy,
        bool ascending,
        Func<T, TKey>? thenBy,
        bool thenAscending)
    {
        IOrderedEnumerable<T> orderedEntities;

        if (orderBy != null)
        {
            orderedEntities = ascending
                ? entities.OrderBy(orderBy)
                : entities.OrderByDescending(orderBy);
        }
        else
        {
            orderedEntities = entities.OrderBy(x => x);
        }

        if (thenBy != null)
        {
            orderedEntities = thenAscending
                ? orderedEntities.ThenBy(thenBy)
                : orderedEntities.ThenByDescending(thenBy);
        }

        return orderedEntities;
    }

    #endregion
}