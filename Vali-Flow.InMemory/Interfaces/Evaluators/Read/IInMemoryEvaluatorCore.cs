using Vali_Flow.Core.Builder;

namespace Vali_Flow.InMemory.Interfaces.Evaluators.Read;

/// <summary>
/// Defines core evaluation methods for single entities and simple collection checks.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate.</typeparam>
public interface IInMemoryEvaluatorCore<T> where T : class
{
    /// <summary>
    /// Evaluates whether a single entity satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates to true.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>True if the entity satisfies the condition; otherwise, false.</returns>
    bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false);

    /// <summary>
    /// Determines whether any entity in the collection satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, evaluates to true for non-empty collections.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>True if at least one entity satisfies the condition; otherwise, false.</returns>
    bool EvaluateAny(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false);

    /// <summary>
    /// Counts the number of entities in the collection that satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, counts all entities.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The number of entities that satisfy the condition.</returns>
    int EvaluateCount(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false);

    /// <summary>
    /// Retrieves the first entity that satisfies the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="valiFlow">The ValiFlow condition to apply. If null, returns the first entity.</param>
    /// <param name="negateCondition">If true, negates the Vali-Flow condition.</param>
    /// <returns>The first entity that satisfies the condition, or null if none satisfy.</returns>
    T? GetFirst(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null, bool negateCondition = false);

    /// <summary>
    /// Retrieves the first entity that does not satisfy the specified Vali-Flow condition.
    /// </summary>
    /// <param name="entities">The collection of entities to evaluate.</param>
    /// <param name="valiFlow">The Vali-Flow condition to apply. If null, returns the first entity.</param>
    /// <returns>The first entity that fails the condition, or null if none fail.</returns>
    T? GetFirstFailed(IEnumerable<T>? entities = null, ValiFlow<T>? valiFlow = null);
}
