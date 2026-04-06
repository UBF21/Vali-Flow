using System.Numerics;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.InMemory.Interfaces.Evaluators.Read;

/// <summary>
/// Defines numeric aggregation methods over in-memory entity collections.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate.</typeparam>
public interface IInMemoryAggregator<T> where T : class
{
    /// <summary>
    /// Computes the minimum value of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The minimum value of the selected property for entities that satisfy the condition.</returns>
    TResult EvaluateMin<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Computes the maximum value of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The maximum value of the selected property for entities that satisfy the condition.</returns>
    TResult EvaluateMax<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Computes the average value of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The average value of the selected property for entities that satisfy the condition, as a decimal.</returns>
    decimal EvaluateAverage<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Computes the sum of a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to sum.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The sum of the selected property values for entities that satisfy the condition.</returns>
    TResult EvaluateSum<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;

    /// <summary>
    /// Applies a custom aggregation to a selected property for entities that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected property, must implement INumber&lt;TResult&gt;.</typeparam>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="selector">A function to extract the property value to aggregate.</param>
    /// <param name="aggregator">A function that defines how to aggregate two values into a single result.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The aggregated result of the selected property values for entities that satisfy the condition.</returns>
    TResult EvaluateAggregate<TResult>(
        IEnumerable<T>? entities,
        Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator,
        ValiFlow<T>? valiFlow = null,
        bool negateCondition = false
    ) where TResult : INumber<TResult>;
}
