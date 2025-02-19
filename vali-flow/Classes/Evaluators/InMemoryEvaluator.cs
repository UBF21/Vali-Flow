using System.Numerics;
using vali_flow.Classes.Base;
using vali_flow.Interfaces.Evaluators;

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
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return compiledCondition(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions.", ex);
        }
    }

    public bool EvaluateAny(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities.Any(compiledCondition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions.", ex);
        }
    }

    public int EvaluateCount(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities.Count(compiledCondition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions.", ex);
        }
    }

    public T? GetFirstFailed(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities.FirstOrDefault(entity => !compiledCondition(entity));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions.", ex);
        }
    }

    public T? GetFirst(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities.FirstOrDefault(entity => compiledCondition(entity));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions.", ex);
        }
    }

    public IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true)
    {
        Func<T, bool> compiledCondition = _builder.BuildNegated().Compile();

        List<T> failedEntities =
            entities.Where(compiledCondition).ToList(); // .ToList() evita múltiples enumeraciones

        IOrderedEnumerable<T> orderedEntities;

        if (orderBy != null)
        {
            orderedEntities = ascending
                ? failedEntities.OrderBy(orderBy)
                : failedEntities.OrderByDescending(orderBy);
        }
        else
        {
            orderedEntities = failedEntities.OrderBy(x => x);
        }

        
        if (thenBy != null)
        {
            orderedEntities = thenAscending
                ? orderedEntities.ThenBy(thenBy)
                : orderedEntities.ThenByDescending(thenBy);
        }

        return orderedEntities;
    }

    public IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true)
    {
        Func<T, bool> compiledCondition = _builder.Build().Compile();

        List<T> passedEntities =
            entities.Where(entity => compiledCondition(entity)).ToList();

        IOrderedEnumerable<T> orderedEntities;

        if (orderBy != null)
        {
            orderedEntities = ascending
                ? passedEntities.OrderBy(orderBy)
                : passedEntities.OrderByDescending(orderBy);
        }
        else
        {
            orderedEntities = passedEntities.OrderBy(x => x); 
        }

        if (thenBy != null)
        {
            orderedEntities = thenAscending
                ? orderedEntities.ThenBy(thenBy)
                : orderedEntities.ThenByDescending(thenBy);
        }

        return orderedEntities;
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
        if (page <= 0)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than zero.");

        Func<T, bool> compiledCondition = _builder.Build().Compile();

        IEnumerable<T> filteredEntities = entities.Where(entity => compiledCondition(entity));

        IOrderedEnumerable<T> orderedEntities;

        if (orderBy != null)
        {
            orderedEntities = ascending
                ? filteredEntities.OrderBy(orderBy)
                : filteredEntities.OrderByDescending(orderBy);
        }
        else
        {
            orderedEntities = filteredEntities.OrderBy(x => x);
        }

        if (thenBy != null)
        {
            orderedEntities = thenAscending
                ? orderedEntities.ThenBy(thenBy)
                : orderedEntities.ThenByDescending(thenBy);
        }

        IEnumerable<T> pagedEntities = orderedEntities
            .Skip((page - 1) * pageSize) 
            .Take(pageSize); 

        return pagedEntities;
    }

    public IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T> entities,
        int count,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than 0.");
        }

        Func<T, bool> compiledCondition = _builder.Build().Compile();

        IEnumerable<T> filteredEntities = entities.Where(entity => compiledCondition(entity));

        IOrderedEnumerable<T> orderedEntities;

        if (orderBy != null)
        {
            orderedEntities = ascending
                ? filteredEntities.OrderBy(orderBy)
                : filteredEntities.OrderByDescending(orderBy);
        }
        else
        {
            orderedEntities = filteredEntities.OrderBy(x => x);
        }

        if (thenBy != null)
        {
            orderedEntities = thenAscending
                ? orderedEntities.ThenBy(thenBy)
                : orderedEntities.ThenByDescending(thenBy);
        }

        return orderedEntities.Take(count);
    }

    public IEnumerable<T> EvaluateDistinct<TKey>(IEnumerable<T> entities, Func<T, TKey> selector)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        Func<T, bool> compiledCondition = _builder.Build().Compile();

        IEnumerable<T> filteredEntities = entities.Where(compiledCondition);

        return filteredEntities
            .GroupBy(selector)
            .Select(group => group.First());
    }

    public IEnumerable<T> EvaluateDuplicates<TKey>(IEnumerable<T> entities, Func<T, TKey> selector)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        return entities
            .GroupBy(selector)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group);
    }

    public int GetFirstMatchIndex(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            int index = 0;
            foreach (T entity in entities)
            {
                if (compiledCondition(entity)) return index;
                index++;
            }
            
            return -1;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the condition for first match.", ex);
        }
    }

    public int GetLastMatchIndex(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            int index = 0;
            int lastIndex = -1;
            foreach (T entity in entities)
            {
                if (compiledCondition(entity)) lastIndex = index;

                index++;
            }

            return lastIndex;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the condition for last match.", ex);
        }
    }

    public T? GetLastFailed(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.BuildNegated().Compile();

            return entities.LastOrDefault(compiledCondition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions for GetLastFailed.", ex);
        }
    }

    public T? GetLast(IEnumerable<T> entities)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.BuildNegated().Compile();

            return entities.LastOrDefault(compiledCondition);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions for GetLast.", ex);
        }
    }

    public TResult EvaluateMin<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(entity => compiledCondition(entity))
                .Select(selector)
                .ToList();

            if (!projectedValues.Any())
                throw new InvalidOperationException("No elements found to evaluate the minimum value.");

            TResult minValue = projectedValues.Min() ?? TResult.Zero;

            return minValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the minimum value.", ex);
        }
    }

    public TResult EvaluateMax<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>

    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(entity => compiledCondition(entity))
                .Select(selector)
                .ToList();

            if (!projectedValues.Any())
                throw new InvalidOperationException("No elements found to evaluate the maximum value.");

            TResult minValue = projectedValues.Max() ?? TResult.Zero;

            return minValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the maximum value.", ex);
        }
    }

    public decimal EvaluateAverage<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(entity => compiledCondition(entity))
                .Select(selector)
                .ToList();

            if (!projectedValues.Any())
                throw new InvalidOperationException("No elements found to evaluate the average value.");

            decimal averageValue = projectedValues.Average(x => Convert.ToDecimal(x));

            return averageValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the average value.", ex);
        }
    }

    public TResult EvaluateSum<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
        where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(entity => compiledCondition(entity))
                .Select(selector)
                .ToList();

            if (!projectedValues.Any())
                throw new InvalidOperationException("No elements found to evaluate the maximum value.");

            TResult sumValue = projectedValues.Aggregate(TResult.Zero, (acc, x) => acc + x);

            return sumValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the maximum value.", ex);
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
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            List<TResult> projectedValues = entities
                .Where(entity => compiledCondition(entity))
                .Select(selector)
                .ToList();

            if (!projectedValues.Any())
                throw new InvalidOperationException("No elements found to evaluate the aggregate value.");

            TResult result = projectedValues.Aggregate(aggregator);

            if (result == null)
                throw new InvalidOperationException("The aggregated value evaluated is null.");

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the aggregate value.", ex);
        }
    }

    public Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities
                .Where(e => compiledCondition(e))
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating grouped elements.", ex);
        }
    }

    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities
                .Where(e => compiledCondition(e))
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating count by group.", ex);
        }
    }

    public Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities
                .Where(e => compiledCondition(e))
                .GroupBy(keySelector)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(selector).Aggregate(TResult.Zero, (acc, x) => acc + x)
                );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating sum by group.", ex);
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
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities
                .Where(e => compiledCondition(e))
                .GroupBy(keySelector)
                .ToDictionary(g =>
                        g.Key,
                    g => g.Select(selector).Min() ?? TResult.Zero);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating minimum by group.", ex);
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
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities
                .Where(e => compiledCondition(e))
                .GroupBy(keySelector)
                .ToDictionary(g =>
                        g.Key,
                    g => g.Select(selector).Max() ?? TResult.Zero);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating maximum by group.", ex);
        }
    }

    public Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        Func<T, TResult> selector) where TKey : notnull where TResult : INumber<TResult>

    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities
                .Where(e => compiledCondition(e))
                .GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.Average(e => Convert.ToDecimal(selector(e))));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating average by group.", ex);
        }
    }

    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector)
        where TKey : notnull
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities
                .Where(e => compiledCondition(e))
                .GroupBy(keySelector)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating duplicates by group.", ex);
        }
    }

    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector)
        where TKey : notnull
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();
            return entities
                .Where(compiledCondition)
                .GroupBy(keySelector)
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating uniques by group.", ex);
        }
    }

    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey> keySelector,
        int count,
        Func<T, object>? orderBy = null,
        bool ascending = true) where TKey : notnull
    {
        try
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");

            Func<T, bool> compiledCondition = _builder.Build().Compile();
            
            return entities
                .Where(compiledCondition)
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
            throw new InvalidOperationException("Error in evaluating top elements by group.", ex);
        }
    }
}