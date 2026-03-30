namespace Vali_Flow.InMemory.Utils;

/// <summary>
/// Provides helper methods for validations.
/// </summary>
public class ValidationHelper
{
    /// <summary>
    /// Validates that the entity is not null.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if the entity is null.</exception>
    public static void ValidateEntityNotNull<T>(T entity) where T : class
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }
    }

    /// <summary>
    /// Validates that the entities sequence is not null.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The entities sequence to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if the entities sequence is null.</exception>
    public static void ValidateEntitiesNotNull<T>(IEnumerable<T> entities)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities), "The collection of entities cannot be null.");
        }
    }

    /// <summary>
    /// Validates that the entities sequence is not empty.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The entities sequence to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the entities sequence is empty.</exception>
    public static void ValidateEntitiesEmpty<T>(IEnumerable<T> entities)
    {
        if (!entities.Any())
        {
            throw new ArgumentException("The collection of entities cannot be empty.", nameof(entities));
        }
    }
}
