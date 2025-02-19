using System.Text.Json;

namespace vali_flow.Utils;

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
    
    public static void ValidateQueryNotNull<T>(IQueryable<T> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
    }

    public static void ValidatePagination(int? page, int? pageSize)
    {
        
        if (page != null && page.Value <= 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

        if (pageSize != null && pageSize.Value <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
    }
}