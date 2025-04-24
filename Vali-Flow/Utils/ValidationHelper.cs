using System.Text.Json;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.Utils;

/// <summary>
/// Provides helper methods for validations.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Checks whether a given string is a valid JSON.
    /// </summary>
    /// <param name="val">The string to validate.</param>
    /// <returns><c>true</c> if the string is a valid JSON; otherwise, <c>false</c>.</returns>
    public static bool IsValidJson(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return false;

        try
        {
            JsonDocument.Parse(val);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that the entity is not null.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if the entity is null.</exception>
    public static void ValidateEntityNotNull<T>(T entity) where T : class // Constraint for reference types
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }
    }

    /// <summary>
    /// Validates if the page number is greater than zero.
    /// </summary>
    /// <param name="page">The page number to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the page number is less than or equal to zero.</exception>
    public static void ValidatePageZero(int page)
    {
        if (page <= Constants.ZeroInt)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
    }

    /// <summary>
    /// Validates if the page size is greater than zero.
    /// </summary>
    /// <param name="pageSize">The page size to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the page size is less than or equal to zero.</exception>
    public static void ValidatePageSizeZero(int pageSize)
    {
        if (pageSize <= Constants.ZeroInt)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than zero.");
    }

    /// <summary>
    /// Validates if the count is greater than zero.
    /// </summary>
    /// <param name="count">The count to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the count is less than or equal to zero.</exception>
    public static void ValidateCountZero(int count)
    {
        if (count <= Constants.ZeroInt)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
    }

    /// <summary>
    /// Validates that the query is not null.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the query.</typeparam>
    /// <param name="query">The query to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if the query is null.</exception>
    public static void ValidateQueryNotNull<T>(IQueryable<T> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
    }

    /// <summary>
    /// Validates pagination parameters.  Checks if page and pageSize are greater than zero, if provided.
    /// </summary>
    /// <param name="page">The page number to validate (nullable).</param>
    /// <param name="pageSize">The page size to validate (nullable).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if either page or pageSize is provided and is less than or equal to zero.</exception>
    public static void ValidatePagination(int? page, int? pageSize)
    {
        if (page is <= 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (pageSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
    }

    /// <summary>
    /// Validates pagination parameters.  Checks if page and pageSize are greater than zero, if provided.
    /// </summary>
    /// <param name="page">The page number to validate.</param>
    /// <param name="pageSize">The page size to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if either page or pageSize is provided and is less than or equal to zero.</exception>
    public static void ValidatePagination(int page, int pageSize)
    {
        if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
    }

    public static void ValidatePaginationBlock(int page, int pageSize, int blockSize)
    {
        ValidatePagination(page, pageSize);
        if (blockSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Block size must be greater than zero.");
    }

    public static void ValidatePaginationBlock(int? page, int? pageSize, int? blockSize)
    {
        ValidatePagination(page, pageSize);
        if (blockSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Block size must be greater than zero.");
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
    /// <exception cref="ArgumentException">  Thrown if the entities sequence is empty.</exception>
    public static void ValidateEntitiesEmpty<T>(IEnumerable<T> entities)
    {
        if (!entities.Any())
        {
            throw new ArgumentException("The collection of entities cannot be empty.", nameof(entities));
        }
    }

    /// <summary>
    /// Validates that the selector function is not null.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <typeparam name="TResult">The type of the result of the selector function.</typeparam>
    /// <param name="selector">The selector function to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if the selector function is null.</exception>
    public static void ValidateSelectorNotNull<T, TResult>(Func<T, TResult> selector)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }
    }
    
    public static void ValidateFilterNotNull<T>(ValiFlow<T> filter)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }
    }
}