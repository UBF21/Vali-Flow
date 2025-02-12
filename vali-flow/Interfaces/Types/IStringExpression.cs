using System.Linq.Expressions;

namespace vali_flow.Interfaces.Types;

/// <summary>
/// Defines a set of validation methods for string properties.
/// </summary>
/// <typeparam name="TBuilder">The type of the builder that supports method chaining.</typeparam>
/// <typeparam name="T">The type of the entity being validated.</typeparam>
public interface IStringExpression<out TBuilder, T>
{
    /// <summary>
    /// Ensures that the selected string has a minimum length.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <param name="minLength">The minimum length allowed.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder MinLength(Expression<Func<T, string?>> selector, int minLength);

    /// <summary>
    /// Ensures that the selected string has a maximum length.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <param name="maxLength">The maximum length allowed.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder MaxLength(Expression<Func<T, string?>> selector, int maxLength);

    /// <summary>
    /// Ensures that the selected string matches the given regular expression pattern.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <param name="pattern">The regular expression pattern.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder RegexMatch(Expression<Func<T, string?>> selector, string pattern);

    /// <summary>
    /// Ensures that the selected string is empty.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder Empty(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string is not empty.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder NotEmpty(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string is a valid email address.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder IsEmail(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string ends with the specified value.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <param name="value">The value that the string should end with.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder EndsWith(Expression<Func<T, string>> selector, string value);

    /// <summary>
    /// Ensures that the selected string starts with the specified value.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <param name="value">The value that the string should start with.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder StartsWith(Expression<Func<T, string>> selector, string value);

    /// <summary>
    /// Ensures that the selected string contains the specified value.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <param name="value">The value that the string should contain.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder Contains(Expression<Func<T, string>> selector, string value);

    /// <summary>
    /// Ensures that the selected string has an exact length.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <param name="length">The exact length required.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder ExactLength(Expression<Func<T, string?>> selector, int length);

    /// <summary>
    /// Ensures that the selected string is equal to the given value, ignoring case.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder EqualsIgnoreCase(Expression<Func<T, string?>> selector, string? value);

    /// <summary>
    /// Ensures that the selected string has no leading or trailing spaces.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder Trimmed(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string contains only digits.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder HasOnlyDigits(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string contains only letters.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder HasOnlyLetters(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string contains both letters and numbers.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder HasLettersAndNumbers(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string contains special characters.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder HasSpecialCharacters(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string is a valid JSON format.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder IsJson(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string is a valid Base64 encoded string.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder IsBase64(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string is a invalid JSON format.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder IsNotJson(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Ensures that the selected string is a invalid Base64 encoded string.
    /// </summary>
    /// <param name="selector">Expression to select the string property.</param>
    /// <returns>The builder instance for method chaining.</returns>
    TBuilder IsNotBase64(Expression<Func<T, string?>> selector);

    /// <summary>
    /// Adds a validation rule to check if any of the specified string properties contain at least one of the given search terms.
    /// </summary>
    /// <param name="value">The search string containing one or more words.</param>
    /// <param name="selectors">The expressions selecting the string properties to search within.</param>
    /// <returns>The current builder instance with the applied validation rule.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="value"/> is null, empty, or contains only whitespace.
    /// Thrown if <paramref name="selectors"/> is empty.
    /// </exception>
    /// <remarks>
    /// - The search string is split into multiple terms using spaces as delimiters.
    /// - The validation succeeds if at least one term is found in any of the specified properties.
    /// - The comparison is case-insensitive.
    /// </remarks>
    TBuilder Contains(string value, params Expression<Func<T, string>>[] selectors);
}