using Vali_Flow.Abstractions.Interfaces;

namespace Vali_Flow.InMemory.Interfaces.Evaluators.Read;

/// <summary>
/// Defines methods for evaluating and querying entities in memory using ValiFlow conditions.
/// Composes <see cref="IInMemoryEvaluatorCore{T}"/>, <see cref="IInMemoryQueryable{T}"/>,
/// <see cref="IInMemoryAggregator{T}"/>, and <see cref="IInMemoryGrouping{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the entities to evaluate.</typeparam>
public interface IInMemoryEvaluatorRead<T> :
    IQueryEvaluator<T>,
    IInMemoryEvaluatorCore<T>,
    IInMemoryQueryable<T>,
    IInMemoryAggregator<T>,
    IInMemoryGrouping<T>
    where T : class
{
}
