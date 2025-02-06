using System.Linq.Expressions;

namespace vali_flow.Interfaces.Types;

public interface IStringExpression<out TBuilder, T>
{
    TBuilder MinLength(Expression<Func<T, string?>> selector, int minLength);
    TBuilder MaxLength(Expression<Func<T, string?>> selector, int maxLength);
    TBuilder RegexMatch(Expression<Func<T, string?>> selector, string pattern);
    TBuilder Empty(Expression<Func<T, string?>> selector);
    TBuilder NotEmpty(Expression<Func<T, string?>> selector);
    TBuilder IsEmail(Expression<Func<T, string?>> selector);
    TBuilder EndsWith(Expression<Func<T, string>> selector, string value);
    TBuilder StartsWith(Expression<Func<T, string>> selector, string value);
    TBuilder Contains(Expression<Func<T, string>> selector, string value);
    TBuilder ExactLength(Expression<Func<T, string?>> selector, int length);
    TBuilder EqualsIgnoreCase(Expression<Func<T, string?>> selector, string? value);
    TBuilder Trimmed(Expression<Func<T, string?>> selector);
    TBuilder HasOnlyDigits(Expression<Func<T, string?>> selector);
    TBuilder HasOnlyLetters(Expression<Func<T, string?>> selector);
    TBuilder HasLettersAndNumbers(Expression<Func<T, string?>> selector);
    TBuilder HasSpecialCharacters(Expression<Func<T, string?>> selector);
    TBuilder IsJson(Expression<Func<T, string?>> selector);
    TBuilder IsBase64(Expression<Func<T, string?>> selector);
}