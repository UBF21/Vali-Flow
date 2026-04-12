namespace Vali_Flow.NoSql.Translators;

/// <summary>
/// Shared helper for applying an optional custom converter before falling back
/// to a built-in type switch inside NoSql filter translators.
/// </summary>
/// <remarks>
/// Two overloads exist to handle the CLR distinction between nullable reference
/// types (<c>TResult?</c> where <c>TResult : class</c> → same CLR type as <c>TResult</c>)
/// and nullable value types (<c>TResult?</c> where <c>TResult : struct</c> → <c>Nullable&lt;TResult&gt;</c>).
/// MongoDB, Redis, and DynamoDB use the class overload; Elasticsearch uses the struct overload
/// because FieldValue (Elastic.Clients.Elasticsearch) is a struct.
/// </remarks>
public static class ConditionValueResolver
{
    /// <summary>
    /// Applies <paramref name="customConverter"/> if set and returns a non-null result;
    /// otherwise delegates to <paramref name="fallback"/>. For reference-type results.
    /// </summary>
    public static TResult Resolve<TResult>(
        object? value,
        Func<object?, TResult?>? customConverter,
        Func<object?, TResult> fallback)
        where TResult : class
    {
        if (customConverter != null)
        {
            var custom = customConverter(value);
            if (custom is not null) return custom;
        }

        return fallback(value);
    }

    /// <summary>
    /// Applies <paramref name="customConverter"/> if set and returns a non-null result;
    /// otherwise delegates to <paramref name="fallback"/>. For value-type results.
    /// </summary>
    public static TResult Resolve<TResult>(
        object? value,
        Func<object?, TResult?>? customConverter,
        Func<object?, TResult> fallback)
        where TResult : struct
    {
        if (customConverter != null)
        {
            var custom = customConverter(value);
            if (custom.HasValue) return custom.Value;
        }

        return fallback(value);
    }
}
