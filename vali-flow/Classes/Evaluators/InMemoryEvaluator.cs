using vali_flow.Classes.Base;
using vali_flow.Interfaces.Evaluators;

namespace vali_flow.Classes.Evaluators;

public class InMemoryEvaluator<TBuilder, T> : IInMemoryEvaluator<T>
    where TBuilder : BaseExpression<TBuilder, T>, IInMemoryEvaluator<T>, new()

{
    private readonly BaseExpression<TBuilder, T> _builder;

    public InMemoryEvaluator(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
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
        Func<T, bool> compiledCondition = _builder.Build().Compile();

        var failedEntities =
            entities.Where(entity => !compiledCondition(entity)).ToList(); // .ToList() evita múltiples enumeraciones

        IOrderedEnumerable<T> orderedEntities;

        if (orderBy != null)
        {
            orderedEntities = ascending
                ? failedEntities.OrderBy(orderBy)
                : failedEntities.OrderByDescending(orderBy);
        }
        else
        {
            // Si no se proporciona orderBy, simplemente usamos los elementos como están
            orderedEntities = failedEntities.OrderBy(x => x); // Esto puede ser un criterio predeterminado
        }

        // Aplicar ThenBy si existe un segundo criterio de ordenación
        if (thenBy != null)
        {
            orderedEntities = thenAscending
                ? orderedEntities.ThenBy(thenBy)
                : orderedEntities.ThenByDescending(thenBy);
        }

        // Retornar los elementos que fallaron
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
            entities.Where(entity => compiledCondition(entity)).ToList(); // .ToList() evita múltiples enumeraciones

        IOrderedEnumerable<T> orderedEntities;

        if (orderBy != null)
        {
            orderedEntities = ascending
                ? passedEntities.OrderBy(orderBy)
                : passedEntities.OrderByDescending(orderBy);
        }
        else
        {
            // Si no se proporciona orderBy, simplemente usamos los elementos como están
            orderedEntities = passedEntities.OrderBy(x => x); // Esto puede ser un criterio predeterminado
        }

        // Aplicar ThenBy si existe un segundo criterio de ordenación
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
        if (page <= 0 || pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(null, "Page and pageSize must be greater than 0.");
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
            // Fallback to default sorting
            orderedEntities = filteredEntities.OrderBy(x => x); // Default sorting logic
        }

        if (thenBy != null)
        {
            orderedEntities = thenAscending
                ? orderedEntities.ThenBy(thenBy)
                : orderedEntities.ThenByDescending(thenBy);
        }

        // Step 4: Apply pagination using Skip and Take
        var pagedEntities = orderedEntities
            .Skip((page - 1) * pageSize) // Skip previous pages
            .Take(pageSize); // Take the requested number of entities

        // Step 5: Return the paged and sorted entities
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

        var filteredEntities = entities.Where(entity => compiledCondition(entity));

        IOrderedEnumerable<T> orderedEntities;

        if (orderBy != null)
        {
            orderedEntities = ascending
                ? filteredEntities.OrderBy(orderBy)
                : filteredEntities.OrderByDescending(orderBy);
        }
        else
        {
            // Fallback to default sorting
            orderedEntities = filteredEntities.OrderBy(x => x); // Default sorting logic
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

        var filteredEntities = entities.Where(compiledCondition);

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
            .Where(group => group.Count() > 1) // Selecciona solo los grupos con más de un elemento
            .SelectMany(group => group);
    }

    public int GetFirstMatchIndex(IEnumerable<T> entities)
    {
        try
        {
            // Compila la condición
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            int index = 0;
            foreach (var entity in entities)
            {
                if (compiledCondition(entity)) return index;
                index++;
            }

            // Si ningún elemento cumple la condición, se devuelve -1.
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
            // Compila la condición
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            int index = 0;
            int lastIndex = -1;
            foreach (var entity in entities)
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
            // Compila la condición
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            // Se invierte la secuencia y se busca el primer elemento que cumple la condición
            return entities.Reverse().FirstOrDefault(entity => !compiledCondition(entity));
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
            // Compila la condición
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            // Se invierte la secuencia y se busca el primer elemento que cumple la condición
            return entities.Reverse().FirstOrDefault(entity => compiledCondition(entity));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions for GetLast.", ex);
        }
    }

    public TResult EvaluateMin<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            var projectedValues = entities
                .Where(entity => compiledCondition(entity))
                .Select(selector)
                .ToList();

            if (!projectedValues.Any())
                throw new InvalidOperationException("No elements found to evaluate the minimum value.");

            var minValue = projectedValues.Min();

            if (minValue == null)
                throw new InvalidOperationException("The minimum value evaluated is null.");

            return minValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the minimum value.", ex);
        }
    }

    public TResult EvaluateMax<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            var projectedValues = entities
                .Where(entity => compiledCondition(entity))
                .Select(selector)
                .ToList();

            if (!projectedValues.Any())
                throw new InvalidOperationException("No elements found to evaluate the maximum value.");

            var minValue = projectedValues.Max();

            if (minValue == null)
                throw new InvalidOperationException("The maximum value evaluated is null.");

            return minValue;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the maximum value.", ex);
        }
    }

    public TResult EvaluateAverage<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            var projectedValues = entities
                .Where(entity => compiledCondition(entity))
                .Select(selector)
                .ToList();

            if (!projectedValues.Any())
                throw new InvalidOperationException("No elements found to evaluate the average value.");

            decimal averageValue = projectedValues.Average(x => Convert.ToDecimal(x));

            return (TResult)Convert.ChangeType(averageValue, typeof(TResult));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the average value.", ex);
        }
    }

    public TResult EvaluateSum<TResult>(IEnumerable<T> entities, Func<T, TResult> selector)
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

            decimal minValue = projectedValues.Sum(x => Convert.ToDecimal(x));

            return (TResult)Convert.ChangeType(minValue, typeof(TResult));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the maximum value.", ex);
        }
    }

    public TResult EvaluateAggregate<TResult>(IEnumerable<T> entities, Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator)
    {
        try
        {
            Func<T, bool> compiledCondition = _builder.Build().Compile();

            var projectedValues = entities
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
        throw new NotImplementedException();
    }

    public Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    public Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
        IEnumerable<T> entities, 
        Func<T, TKey> keySelector, 
        Func<T, TResult> selector) where TKey : notnull where TResult : struct
    {
        throw new NotImplementedException();
    }

    public Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(IEnumerable<T> entities, Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : struct, IComparable<TResult>
    {
        throw new NotImplementedException();
    }

    public Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(IEnumerable<T> entities, Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : struct, IComparable<TResult>
    {
        throw new NotImplementedException();
    }

    public Dictionary<TKey, double> EvaluateAverageByGroup<TKey, TResult>(IEnumerable<T> entities, Func<T, TKey> keySelector, Func<T, TResult> selector) where TKey : notnull where TResult : struct
    {
        throw new NotImplementedException();
    }

    public Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    public Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector) where TKey : notnull
    {
        throw new NotImplementedException();
    }

    public Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector, int count, Func<T, object>? orderBy = null,
        bool ascending = true) where TKey : notnull
    {
        throw new NotImplementedException();
    }
}