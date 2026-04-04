namespace Vali_Flow.Abstractions.Interfaces;

/// <summary>
/// Full provider-agnostic contract combining <see cref="IQueryReader{T}"/> and <see cref="IQueryAggregator{T}"/>.
/// </summary>
/// <remarks>
/// Preserved as a convenience interface for existing code.
/// Implementors that need only read operations may implement <see cref="IQueryReader{T}"/> directly.
/// Implementors that need only aggregation may implement <see cref="IQueryAggregator{T}"/> directly.
/// </remarks>
/// <typeparam name="T">The entity / document type.</typeparam>
public interface IQueryEvaluator<T> : IQueryReader<T>, IQueryAggregator<T> where T : class { }
