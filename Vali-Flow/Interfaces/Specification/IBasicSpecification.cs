namespace Vali_Flow.Interfaces.Specification;

/// <summary>
/// Represents a basic specification interface that defines filtering, inclusion, and configuration options 
/// for querying entities of type <typeparamref name="T"/>. This interface extends <see cref="ISpecification{T}"/>
/// and is intended for simple query scenarios where basic filtering and eager loading of related data are required.
/// </summary>
/// <typeparam name="T">The type of entity to which the specification applies, constrained to be a class.</typeparam>
/// <remarks>
/// This interface is designed for scenarios where the query does not require advanced ordering or pagination 
/// but still needs to support includes and no-tracking options. Implementations should provide methods 
/// to define filters and include related properties efficiently.
/// </remarks>
public interface IBasicSpecification<T> : ISpecification<T> where T : class
{
    
}