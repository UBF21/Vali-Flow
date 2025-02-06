using System.Linq.Expressions;
using System.Text.RegularExpressions;
using vali_flow.Classes.Base;
using vali_flow.Interfaces.Types;
using vali_flow.RegularExpressions;
using vali_flow.Utils;

namespace vali_flow.Classes.Types;

public class StringExpression<TBuilder, T> : IStringExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>, IStringExpression<TBuilder, T>, new()
{
    private readonly BaseExpression<TBuilder, T> _builder;

    public StringExpression(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
    }

    public TBuilder MinLength(Expression<Func<T, string?>> selector, int minLength)
    {
        Expression<Func<string?, bool>> predicate = val => !string.IsNullOrEmpty(val) && val.Length >= minLength;
        return _builder.Add(selector, predicate);
    }

    public TBuilder MaxLength(Expression<Func<T, string?>> selector, int maxLength)
    {
        Expression<Func<string?, bool>> predicate = val => !string.IsNullOrEmpty(val) && val.Length <= maxLength;
        return _builder.Add(selector, predicate);
    }

    public TBuilder RegexMatch(Expression<Func<T, string?>> selector, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

        Expression<Func<string?, bool>> predicate = value =>
            !string.IsNullOrEmpty(value) && Regex.IsMatch(value, pattern);
        return _builder.Add(selector, predicate);
    }

    public TBuilder Empty(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val => string.IsNullOrEmpty(val);
        return _builder.Add(selector, predicate);
    }

    public TBuilder NotEmpty(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val => !string.IsNullOrEmpty(val);
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsEmail(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val =>
            !string.IsNullOrEmpty(val) && RegularExpression.FormatEmail.IsMatch(val);
        return _builder.Add(selector, predicate);
    }

    public TBuilder EndsWith(Expression<Func<T, string>> selector, string value)
    {
        Expression<Func<string, bool>> predicate = val => val.EndsWith(value);
        return _builder.Add(selector, predicate);
    }

    public TBuilder StartsWith(Expression<Func<T, string>> selector, string value)
    {
        Expression<Func<string, bool>> predicate = val => val.StartsWith(value);
        return _builder.Add(selector, predicate);
    }

    public TBuilder Contains(Expression<Func<T, string>> selector, string value)
    {
        Expression<Func<string, bool>> predicate = val => val.Contains(value);
        return _builder.Add(selector, predicate);
    }

    public TBuilder ExactLength(Expression<Func<T, string?>> selector, int length)
    {
        Expression<Func<string?, bool>> predicate = val => val != null && val.Length == length;
        return _builder.Add(selector, predicate);
    }

    public TBuilder EqualsIgnoreCase(Expression<Func<T, string?>> selector, string? value)
    {
        Expression<Func<string?, bool>> predicate = val =>
            val != null && value != null && val.Equals(value, StringComparison.OrdinalIgnoreCase);
        return _builder.Add(selector, predicate);
    }

    public TBuilder Trimmed(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val => val != null && val == val.Trim();
        return _builder.Add(selector, predicate);
    }

    public TBuilder HasOnlyDigits(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val => !string.IsNullOrEmpty(val) && val.All(char.IsDigit);
        return _builder.Add(selector, predicate);
    }

    public TBuilder HasOnlyLetters(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val => !string.IsNullOrEmpty(val) && val.All(char.IsLetter);
        return _builder.Add(selector, predicate);
    }

    public TBuilder HasLettersAndNumbers(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val =>
            !string.IsNullOrEmpty(val) && val.Any(char.IsLetter) && val.Any(char.IsDigit);
        return _builder.Add(selector, predicate);
    }

    public TBuilder HasSpecialCharacters(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val =>
            !string.IsNullOrEmpty(val) && val.Any(c => !char.IsLetterOrDigit(c));
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsJson(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?, bool>> predicate = val => ValidationHelper.IsValidJson(val);
        return _builder.Add(selector, predicate);
    }

    public TBuilder IsBase64(Expression<Func<T, string?>> selector)
    {
        Expression<Func<string?,bool>> predicate = val => !string.IsNullOrEmpty(val) && val.Length % 4 == 0 && RegularExpression.FormatBase64.IsMatch(val);
        return _builder.Add(selector, predicate);
    }
}