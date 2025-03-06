using System.Linq.Expressions;
using ValiFlow.Core.Classes.Base;
using ValiFlow.Core.Classes.General;
using ValiFlow.Core.Classes.Types;
using ValiFlow.Core.Interfaces.General;
using ValiFlow.Core.Interfaces.Types;

namespace ValiFlow.Core.Builder;

public class ValiFlow<T>: BaseExpression<ValiFlow<T>, T>,
    IBooleanExpression<ValiFlow<T>, T>, IComparisonExpression<ValiFlow<T>, T>,
    ICollectionExpression<ValiFlow<T>, T>, IStringExpression<ValiFlow<T>, T>,
    INumericExpression<ValiFlow<T>, T>
{
    private readonly IBooleanExpression<ValiFlow<T>, T> _booleanExpression;
    private readonly ICollectionExpression<ValiFlow<T>, T> _collectionExpression;
    private readonly IComparisonExpression<ValiFlow<T>, T> _comparisonExpression;
    private readonly IStringExpression<ValiFlow<T>, T> _stringExpression;
    private readonly INumericExpression<ValiFlow<T>, T> _numericExpression;

    public ValiFlow()
    {
        _booleanExpression = new BooleanExpression<ValiFlow<T>, T>(this);
        _collectionExpression = new CollectionExpression<ValiFlow<T>, T>(this);
        _comparisonExpression = new ComparisonExpression<ValiFlow<T>, T>(this);
        _stringExpression = new StringExpression<ValiFlow<T>, T>(this);
        _numericExpression = new NumericExpression<ValiFlow<T>, T>(this);
    }

    public ValiFlow<T> IsTrue(Func<T, bool> selector) => _booleanExpression.IsTrue(selector);

    public ValiFlow<T> IsFalse(Func<T, bool> selector) => _booleanExpression.IsFalse(selector);

    public ValiFlow<T> NotNull<TProperty>(Expression<Func<T, TProperty?>> selector) => _comparisonExpression.NotNull(selector);

    public ValiFlow<T> Null<TProperty>(Expression<Func<T, TProperty?>> selector) => _comparisonExpression.Null(selector);

    public ValiFlow<T> EqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        where TValue : IEquatable<TValue> => _comparisonExpression.EqualTo(selector, value);

    public ValiFlow<T> NotEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        where TValue : IEquatable<TValue> => _comparisonExpression.NotEqualTo(selector, value);

    public ValiFlow<T> NotEmpty<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector) =>
        _collectionExpression.NotEmpty(selector);

    public ValiFlow<T> In<TValue>(Expression<Func<T, TValue>> selector, IEnumerable<TValue> values)
        => _collectionExpression.In(selector, values);

    public ValiFlow<T> NotIn<TValue>(Expression<Func<T, TValue>> selector, IEnumerable<TValue> values)
        => _collectionExpression.NotIn(selector, values);

    public ValiFlow<T> Count<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector, int count)
        => _collectionExpression.Count(selector, count);

    public ValiFlow<T> CountBetween<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, int min, int max)
        => _collectionExpression.CountBetween(selector, min, max);

    public ValiFlow<T> All<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector,
        Expression<Func<TValue, bool>> predicate)
        => _collectionExpression.All(selector, predicate);

    public ValiFlow<T> Any<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector,
        Expression<Func<TValue, bool>> predicate)
        => _collectionExpression.Any(selector, predicate);

    public ValiFlow<T> Contains<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector, TValue value)
        => _collectionExpression.Contains(selector, value);

    public ValiFlow<T> DistinctCount<TValue>(Expression<Func<T, IEnumerable<TValue?>>> selector, int count)
        => _collectionExpression.DistinctCount(selector, count);

    public ValiFlow<T> None<TValue>(Expression<Func<T, IEnumerable<TValue>>> selector,
        Expression<Func<TValue, bool>> predicate)
        => _collectionExpression.None(selector, predicate);

    public ValiFlow<T> MinLength(Expression<Func<T, string?>> selector, int minLength)
        => _stringExpression.MinLength(selector, minLength);

    public ValiFlow<T> MaxLength(Expression<Func<T, string?>> selector, int maxLength)
        => _stringExpression.MaxLength(selector, maxLength);

    public ValiFlow<T> RegexMatch(Expression<Func<T, string?>> selector, string pattern)
        => _stringExpression.RegexMatch(selector, pattern);

    public ValiFlow<T> Empty(Expression<Func<T, string?>> selector)
        => _stringExpression.Empty(selector);

    public ValiFlow<T> NotEmpty(Expression<Func<T, string?>> selector)
        => _stringExpression.NotEmpty(selector);

    public ValiFlow<T> IsEmail(Expression<Func<T, string?>> selector)
        => _stringExpression.IsEmail(selector);

    public ValiFlow<T> EndsWith(Expression<Func<T, string>> selector, string value)
        => _stringExpression.EndsWith(selector, value);

    public ValiFlow<T> StartsWith(Expression<Func<T, string>> selector, string value)
        => _stringExpression.StartsWith(selector, value);

    public ValiFlow<T> Contains(Expression<Func<T, string>> selector, string value)
        => _stringExpression.Contains(selector, value);

    public ValiFlow<T> ExactLength(Expression<Func<T, string?>> selector, int length)
        => _stringExpression.ExactLength(selector, length);

    public ValiFlow<T> EqualsIgnoreCase(Expression<Func<T, string?>> selector, string? value)
        => _stringExpression.EqualsIgnoreCase(selector, value);

    public ValiFlow<T> Trimmed(Expression<Func<T, string?>> selector)
        => _stringExpression.Trimmed(selector);

    public ValiFlow<T> HasOnlyDigits(Expression<Func<T, string?>> selector)
        => _stringExpression.HasOnlyDigits(selector);

    public ValiFlow<T> HasOnlyLetters(Expression<Func<T, string?>> selector)
        => _stringExpression.HasOnlyLetters(selector);

    public ValiFlow<T> HasLettersAndNumbers(Expression<Func<T, string?>> selector)
        => _stringExpression.HasLettersAndNumbers(selector);

    public ValiFlow<T> HasSpecialCharacters(Expression<Func<T, string?>> selector)
        => _stringExpression.HasSpecialCharacters(selector);

    public ValiFlow<T> IsJson(Expression<Func<T, string?>> selector)
        => _stringExpression.IsJson(selector);

    public ValiFlow<T> IsBase64(Expression<Func<T, string?>> selector)
        => _stringExpression.IsBase64(selector);

    public ValiFlow<T> IsNotJson(Expression<Func<T, string?>> selector) 
        => _stringExpression.IsNotJson(selector);

    public ValiFlow<T> IsNotBase64(Expression<Func<T, string?>> selector) 
        => _stringExpression.IsNotBase64(selector);

    public ValiFlow<T> Contains(string value, params Expression<Func<T, string>>[] selectors) 
        => _stringExpression.Contains(value, selectors);

    public ValiFlow<T> Zero(Expression<Func<T, int>> selector)
        => _numericExpression.Zero(selector);

    public ValiFlow<T> Zero(Expression<Func<T, long>> selector)
        => _numericExpression.Zero(selector);

    public ValiFlow<T> Zero(Expression<Func<T, float>> selector)
        => _numericExpression.Zero(selector);

    public ValiFlow<T> Zero(Expression<Func<T, double>> selector)
        => _numericExpression.Zero(selector);

    public ValiFlow<T> Zero(Expression<Func<T, decimal>> selector)
        => _numericExpression.Zero(selector);

    public ValiFlow<T> Zero(Expression<Func<T, short>> selector)
        => _numericExpression.Zero(selector);

    public ValiFlow<T> NotZero(Expression<Func<T, int>> selector)
        => _numericExpression.NotZero(selector);

    public ValiFlow<T> NotZero(Expression<Func<T, long>> selector)
        => _numericExpression.NotZero(selector);
    
    public ValiFlow<T> NotZero(Expression<Func<T, float>> selector)
        => _numericExpression.NotZero(selector);
    
    public ValiFlow<T> NotZero(Expression<Func<T, double>> selector)
        => _numericExpression.NotZero(selector);

    public ValiFlow<T> NotZero(Expression<Func<T, decimal>> selector)
        => _numericExpression.NotZero(selector);
    
    public ValiFlow<T> NotZero(Expression<Func<T, short>> selector)
        => _numericExpression.NotZero(selector);
    
    public ValiFlow<T> InRange(Expression<Func<T, int>> selector, int min, int max)
        => _numericExpression.InRange(selector, min, max);
    
    public ValiFlow<T> InRange(Expression<Func<T, long>> selector, long min, long max)
        => _numericExpression.InRange(selector, min, max);
    
    public ValiFlow<T> InRange(Expression<Func<T, float>> selector, float min, float max)
        => _numericExpression.InRange(selector, min, max);

    public ValiFlow<T> InRange(Expression<Func<T, double>> selector, double min, double max)
        => _numericExpression.InRange(selector, min, max);

    public ValiFlow<T> InRange(Expression<Func<T, decimal>> selector, decimal min, decimal max)
        => _numericExpression.InRange(selector, min, max);
    
    public ValiFlow<T> InRange(Expression<Func<T, short>> selector, short min, short max)
        => _numericExpression.InRange(selector, min, max);

    public ValiFlow<T> InRange(Expression<Func<T, int>> selector, Expression<Func<T, int>> minSelector,
        Expression<Func<T, int>> maxSelector)
        => _numericExpression.InRange(selector, minSelector, maxSelector);
    
    public ValiFlow<T> InRange(Expression<Func<T, long>> selector, Expression<Func<T, long>> minSelector,
        Expression<Func<T, long>> maxSelector)
        => _numericExpression.InRange(selector, minSelector, maxSelector);
    
    public ValiFlow<T> InRange(Expression<Func<T, float>> selector, Expression<Func<T, float>> minSelector,
        Expression<Func<T, float>> maxSelector)
        => _numericExpression.InRange(selector, minSelector, maxSelector);
    
    public ValiFlow<T> InRange(Expression<Func<T, double>> selector, Expression<Func<T, double>> minSelector,
        Expression<Func<T, double>> maxSelector)
        => _numericExpression.InRange(selector, minSelector, maxSelector);

    public ValiFlow<T> InRange(Expression<Func<T, decimal>> selector, Expression<Func<T, decimal>> minSelector,
        Expression<Func<T, decimal>> maxSelector)
        => _numericExpression.InRange(selector, minSelector, maxSelector);

    public ValiFlow<T> InRange(Expression<Func<T, short>> selector, Expression<Func<T, short>> minSelector,
        Expression<Func<T, short>> maxSelector)
        => _numericExpression.InRange(selector, minSelector, maxSelector);
    
    public ValiFlow<T> GreaterThan(Expression<Func<T, int>> selector, int value)
        => _numericExpression.GreaterThan(selector, value);

    public ValiFlow<T> GreaterThan(Expression<Func<T, long>> selector, long value)
        => _numericExpression.GreaterThan(selector, value);
    
    public ValiFlow<T> GreaterThan(Expression<Func<T, float>> selector, float value)
        => _numericExpression.GreaterThan(selector, value);
    
    public ValiFlow<T> GreaterThan(Expression<Func<T, double>> selector, double value)
        => _numericExpression.GreaterThan(selector, value);

    public ValiFlow<T> GreaterThan(Expression<Func<T, decimal>> selector, decimal value)
        => _numericExpression.GreaterThan(selector, value);
    
    public ValiFlow<T> GreaterThan(Expression<Func<T, short>> selector, short value)
        => _numericExpression.GreaterThan(selector, value);

    public ValiFlow<T> GreaterThanOrEqualTo(Expression<Func<T, int>> selector, int value)
        => _numericExpression.GreaterThanOrEqualTo(selector, value);

    public ValiFlow<T> GreaterThanOrEqualTo(Expression<Func<T, long>> selector, long value)
        => _numericExpression.GreaterThanOrEqualTo(selector, value);
    
    public ValiFlow<T> GreaterThanOrEqualTo(Expression<Func<T, float>> selector, float value)
        => _numericExpression.GreaterThanOrEqualTo(selector, value);

    public ValiFlow<T> GreaterThanOrEqualTo(Expression<Func<T, double>> selector, double value)
        => _numericExpression.GreaterThanOrEqualTo(selector, value);

    public ValiFlow<T> GreaterThanOrEqualTo(Expression<Func<T, decimal>> selector, decimal value)
        => _numericExpression.GreaterThanOrEqualTo(selector, value);

    public ValiFlow<T> GreaterThanOrEqualTo(Expression<Func<T, short>> selector, short value)
        => _numericExpression.GreaterThanOrEqualTo(selector, value);

    public ValiFlow<T> LessThan(Expression<Func<T, int>> selector, int value)
        => _numericExpression.LessThan(selector, value);
    
    public ValiFlow<T> LessThan(Expression<Func<T, long>> selector, long value)
        => _numericExpression.LessThan(selector, value);
    
    public ValiFlow<T> LessThan(Expression<Func<T, float>> selector, float value)
        => _numericExpression.LessThan(selector, value);
    
    public ValiFlow<T> LessThan(Expression<Func<T, double>> selector, double value)
        => _numericExpression.LessThan(selector, value);
    
    public ValiFlow<T> LessThan(Expression<Func<T, decimal>> selector, decimal value)
        => _numericExpression.LessThan(selector, value);

    public ValiFlow<T> LessThan(Expression<Func<T, short>> selector, short value)
        => _numericExpression.LessThan(selector, value);

    public ValiFlow<T> LessThanOrEqualTo(Expression<Func<T, int>> selector, int value)
        => _numericExpression.LessThanOrEqualTo(selector, value);
    
    public ValiFlow<T> LessThanOrEqualTo(Expression<Func<T, long>> selector, long value)
        => _numericExpression.LessThanOrEqualTo(selector, value);
    
    public ValiFlow<T> LessThanOrEqualTo(Expression<Func<T, float>> selector, float value)
        => _numericExpression.LessThanOrEqualTo(selector, value);
    
    public ValiFlow<T> LessThanOrEqualTo(Expression<Func<T, double>> selector, double value)
        => _numericExpression.LessThanOrEqualTo(selector, value);

    public ValiFlow<T> LessThanOrEqualTo(Expression<Func<T, decimal>> selector, decimal value)
        => _numericExpression.LessThanOrEqualTo(selector, value);

    public ValiFlow<T> LessThanOrEqualTo(Expression<Func<T, short>> selector, short value)
        => _numericExpression.LessThanOrEqualTo(selector, value);
    
    public ValiFlow<T> Positive(Expression<Func<T, int>> selector)
        => _numericExpression.Positive(selector);
    
    public ValiFlow<T> Positive(Expression<Func<T, long>> selector)
        => _numericExpression.Positive(selector);
    
    public ValiFlow<T> Positive(Expression<Func<T, float>> selector)
        => _numericExpression.Positive(selector);

    public ValiFlow<T> Positive(Expression<Func<T, double>> selector)
        => _numericExpression.Positive(selector);
    
    public ValiFlow<T> Positive(Expression<Func<T, decimal>> selector)
        => _numericExpression.Positive(selector);
    
    public ValiFlow<T> Positive(Expression<Func<T, short>> selector)
        => _numericExpression.Positive(selector);

    public ValiFlow<T> Negative(Expression<Func<T, int>> selector)
        => _numericExpression.Negative(selector);
    
    public ValiFlow<T> Negative(Expression<Func<T, long>> selector)
        => _numericExpression.Negative(selector);
    
    public ValiFlow<T> Negative(Expression<Func<T, float>> selector)
        => _numericExpression.Negative(selector);

    public ValiFlow<T> Negative(Expression<Func<T, double>> selector)
        => _numericExpression.Negative(selector);
    
    public ValiFlow<T> Negative(Expression<Func<T, decimal>> selector)
        => _numericExpression.Negative(selector);
    
    public ValiFlow<T> Negative(Expression<Func<T, short>> selector)
        => _numericExpression.Negative(selector);
    
    public ValiFlow<T> MinValue(Expression<Func<T, int>> selector, int minValue)
        => _numericExpression.MinValue(selector, minValue);
    
    public ValiFlow<T> MinValue(Expression<Func<T, long>> selector, long minValue)
        => _numericExpression.MinValue(selector, minValue);

    public ValiFlow<T> MinValue(Expression<Func<T, float>> selector, float minValue)
        => _numericExpression.MinValue(selector, minValue);

    public ValiFlow<T> MinValue(Expression<Func<T, double>> selector, double minValue)
        => _numericExpression.MinValue(selector, minValue);
    
    public ValiFlow<T> MinValue(Expression<Func<T, decimal>> selector, decimal minValue)
        => _numericExpression.MinValue(selector, minValue);
    
    public ValiFlow<T> MinValue(Expression<Func<T, short>> selector, short minValue)
        => _numericExpression.MinValue(selector, minValue);
    
    public ValiFlow<T> MaxValue(Expression<Func<T, int>> selector, int maxValue)
        => _numericExpression.MinValue(selector, maxValue);
    
    public ValiFlow<T> MaxValue(Expression<Func<T, long>> selector, long maxValue)
        => _numericExpression.MinValue(selector, maxValue);

    public ValiFlow<T> MaxValue(Expression<Func<T, float>> selector, float maxValue)
        => _numericExpression.MinValue(selector, maxValue);

    public ValiFlow<T> MaxValue(Expression<Func<T, double>> selector, double maxValue)
        => _numericExpression.MinValue(selector, maxValue);
    
    public ValiFlow<T> MaxValue(Expression<Func<T, decimal>> selector, decimal maxValue)
        => _numericExpression.MinValue(selector, maxValue);
    
    public ValiFlow<T> MaxValue(Expression<Func<T, short>> selector, short maxValue)
        => _numericExpression.MinValue(selector, maxValue);
}