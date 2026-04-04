using System.Linq.Expressions;
using Vali_Flow.Sql.Dialects;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for a SQL WHERE clause. Mirrors the ValiFlow.Core condition-chaining API
/// but generates parameterized SQL instead of expression trees.
/// </summary>
/// <remarks>
/// Conditions are grouped using the same AND/OR grouping algorithm as ValiFlow.Core:
/// each call to <see cref="Or"/> starts a new OR group; conditions within a group are ANDed.
/// Parameters use the <c>pw</c> prefix (e.g. <c>@pw0</c>) to avoid collisions with
/// other parameter sources (ExpressionToSqlVisitor uses <c>@p</c>).
/// </remarks>
/// <example>
/// <code>
/// var where = new SqlWhereBuilder&lt;Product&gt;()
///     .GreaterThan(p => p.Price, 100m)
///     .LessThan(p => p.Price, 1000m)
///     .Or()
///     .EqualTo(p => p.Category, "Premium");
///
/// // SQL: (([Price] > @pw0 AND [Price] &lt; @pw1) OR [Category] = @pw2)
/// </code>
/// </example>
public sealed class SqlWhereBuilder<T> : SqlConditionBuilderBase<SqlWhereBuilder<T>, T>
    where T : class
{
    protected override string ParamPrefix => "pw";

    /// <summary>Creates a new root WHERE builder.</summary>
    public SqlWhereBuilder() { }

    private SqlWhereBuilder(SharedState state) : base(state) { }

    // ── Comparison ────────────────────────────────────────────────────────────

    /// <summary>Adds <c>[col] = @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> EqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{d.QuoteIdentifier(col)} = {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] != @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> NotEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{d.QuoteIdentifier(col)} != {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] > @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> GreaterThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{d.QuoteIdentifier(col)} > {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] >= @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> GreaterThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{d.QuoteIdentifier(col)} >= {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] &lt; @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> LessThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{d.QuoteIdentifier(col)} < {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] &lt;= @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> LessThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{d.QuoteIdentifier(col)} <= {d.ParameterPrefix}{param}");
    }

    // ── Raw expression ────────────────────────────────────────────────────────

    /// <summary>
    /// Translates a raw boolean lambda to SQL using <see cref="Translators.ExpressionToSqlVisitor"/>.
    /// Supports the same expressions as <c>ValiFlow&lt;T&gt;</c>: comparisons, LIKE, null checks, boolean members, etc.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Add(x => x.Age > 18 &amp;&amp; x.IsActive)
    /// </code>
    /// </example>
    public SqlWhereBuilder<T> Add(Expression<Func<T, bool>> condition)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        return AddCondition(d =>
        {
            var result = Translators.ExpressionToSqlVisitor.Translate(condition, d);
            string sql = result.Sql;
            // Remap visitor params (p0, p1...) to pw-prefixed params to avoid key collisions
            foreach (var (key, value) in result.Parameters.OrderByDescending(x => x.Key.Length))
            {
                string newName = $"pw{_state.ParamIndex++}";
                sql = sql.Replace($"{d.ParameterPrefix}{key}", $"{d.ParameterPrefix}{newName}");
                _state.Parameters[newName] = value;
            }
            return sql;
        });
    }

    /// <summary>
    /// Conditionally adds a boolean lambda — only when <paramref name="when"/> is <c>true</c>.
    /// Useful for optional filters driven by request parameters.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AddIf(request.MinPrice.HasValue, x => x.Price >= request.MinPrice!.Value)
    /// </code>
    /// </example>
    public SqlWhereBuilder<T> AddIf(bool when, Expression<Func<T, bool>> condition)
    {
        if (!when) return this;
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        return Add(condition);
    }

    /// <summary>
    /// Conditionally adds a sub-group — only when <paramref name="when"/> is <c>true</c>.
    /// </summary>
    public SqlWhereBuilder<T> AddIf(bool when, Action<SqlWhereBuilder<T>> configure)
    {
        if (!when) return this;
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        return AddSubGroup(configure);
    }

    // ── Null ──────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>[col] IS NULL</c>.</summary>
    public SqlWhereBuilder<T> IsNull<TValue>(Expression<Func<T, TValue>> selector)
    {
        var col = GetName(selector);
        return AddCondition(d => d.NullCheck(d.QuoteIdentifier(col)));
    }

    /// <summary>Adds <c>[col] IS NOT NULL</c>.</summary>
    public SqlWhereBuilder<T> IsNotNull<TValue>(Expression<Func<T, TValue>> selector)
    {
        var col = GetName(selector);
        return AddCondition(d => d.NotNullCheck(d.QuoteIdentifier(col)));
    }

    // ── LIKE ──────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>[col] LIKE '%value%'</c>.</summary>
    public SqlWhereBuilder<T> Contains(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"%{EscapeLikePattern(value)}%");
        return AddCondition(d => $"{d.QuoteIdentifier(col)} {d.LikeOperator} {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] NOT LIKE '%value%'</c>.</summary>
    public SqlWhereBuilder<T> NotContains(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"%{EscapeLikePattern(value)}%");
        return AddCondition(d => $"{d.QuoteIdentifier(col)} NOT {d.LikeOperator} {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] LIKE 'value%'</c>.</summary>
    public SqlWhereBuilder<T> StartsWith(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"{EscapeLikePattern(value)}%");
        return AddCondition(d => $"{d.QuoteIdentifier(col)} {d.LikeOperator} {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] LIKE '%value'</c>.</summary>
    public SqlWhereBuilder<T> EndsWith(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"%{EscapeLikePattern(value)}");
        return AddCondition(d => $"{d.QuoteIdentifier(col)} {d.LikeOperator} {d.ParameterPrefix}{param}");
    }

    // ── BETWEEN ───────────────────────────────────────────────────────────────

    /// <summary>Adds <c>[col] BETWEEN @pw{n} AND @pw{n+1}</c>.</summary>
    public SqlWhereBuilder<T> Between<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
    {
        var col = GetName(selector);
        var fromParam = AddParam(from);
        var toParam = AddParam(to);
        return AddCondition(d =>
            $"{d.QuoteIdentifier(col)} BETWEEN {d.ParameterPrefix}{fromParam} AND {d.ParameterPrefix}{toParam}");
    }

    /// <summary>Adds <c>[col] NOT BETWEEN @pw{n} AND @pw{n+1}</c>.</summary>
    public SqlWhereBuilder<T> NotBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
    {
        var col = GetName(selector);
        var fromParam = AddParam(from);
        var toParam = AddParam(to);
        return AddCondition(d =>
            $"{d.QuoteIdentifier(col)} NOT BETWEEN {d.ParameterPrefix}{fromParam} AND {d.ParameterPrefix}{toParam}");
    }

    // ── IN ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds <c>[col] IN (@pw{n}, ...)</c>.
    /// An empty collection emits <c>1=0</c> (always false — matches SQL semantics).
    /// </summary>
    public SqlWhereBuilder<T> In<TValue>(Expression<Func<T, TValue>> selector, IEnumerable<TValue> values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var col = GetName(selector);
        var list = values.ToList();
        if (list.Count == 0)
            return AddCondition(_ => "1=0");
        var paramNames = list.Select(v => AddParam(v)).ToList();
        return AddCondition(d =>
        {
            var inList = string.Join(", ", paramNames.Select(p => $"{d.ParameterPrefix}{p}"));
            return $"{d.QuoteIdentifier(col)} IN ({inList})";
        });
    }

    /// <summary>
    /// Adds <c>[col] NOT IN (@pw{n}, ...)</c>.
    /// An empty collection emits <c>1=1</c> (always true — NOT IN empty set is vacuously true).
    /// </summary>
    public SqlWhereBuilder<T> NotIn<TValue>(Expression<Func<T, TValue>> selector, IEnumerable<TValue> values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var col = GetName(selector);
        var list = values.ToList();
        if (list.Count == 0)
            return AddCondition(_ => "1=1");
        var paramNames = list.Select(v => AddParam(v)).ToList();
        return AddCondition(d =>
        {
            var inList = string.Join(", ", paramNames.Select(p => $"{d.ParameterPrefix}{p}"));
            return $"{d.QuoteIdentifier(col)} NOT IN ({inList})";
        });
    }

    // ── Case-insensitive LIKE ─────────────────────────────────────────────────

    /// <summary>Case-insensitive <c>LIKE '%value%'</c>. Uses <c>ILikeExpression</c> from the dialect.</summary>
    public SqlWhereBuilder<T> ContainsIgnoreCase(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"%{EscapeLikePattern(value)}%");
        return AddCondition(d => d.ILikeExpression(d.QuoteIdentifier(col), $"{d.ParameterPrefix}{param}"));
    }

    /// <summary>Case-insensitive <c>LIKE 'value%'</c>.</summary>
    public SqlWhereBuilder<T> StartsWithIgnoreCase(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"{EscapeLikePattern(value)}%");
        return AddCondition(d => d.ILikeExpression(d.QuoteIdentifier(col), $"{d.ParameterPrefix}{param}"));
    }

    /// <summary>Case-insensitive <c>LIKE '%value'</c>.</summary>
    public SqlWhereBuilder<T> EndsWithIgnoreCase(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"%{EscapeLikePattern(value)}");
        return AddCondition(d => d.ILikeExpression(d.QuoteIdentifier(col), $"{d.ParameterPrefix}{param}"));
    }

    // ── Date part filtering ───────────────────────────────────────────────────

    /// <summary>Adds <c>YEAR([col]) = @pw{n}</c> (or dialect equivalent).</summary>
    public SqlWhereBuilder<T> DatePartEquals(Expression<Func<T, object>> selector, string part, int value)
        => AddDatePartComparison(selector, part, "=", value);

    /// <summary>Adds <c>YEAR([col]) > @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> DatePartGreaterThan(Expression<Func<T, object>> selector, string part, int value)
        => AddDatePartComparison(selector, part, ">", value);

    /// <summary>Adds <c>YEAR([col]) >= @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> DatePartGreaterThanOrEqualTo(Expression<Func<T, object>> selector, string part, int value)
        => AddDatePartComparison(selector, part, ">=", value);

    /// <summary>Adds <c>YEAR([col]) &lt; @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> DatePartLessThan(Expression<Func<T, object>> selector, string part, int value)
        => AddDatePartComparison(selector, part, "<", value);

    /// <summary>Adds <c>YEAR([col]) &lt;= @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> DatePartLessThanOrEqualTo(Expression<Func<T, object>> selector, string part, int value)
        => AddDatePartComparison(selector, part, "<=", value);

    /// <summary>Adds <c>YEAR([col]) BETWEEN @pw{n} AND @pw{n+1}</c>.</summary>
    public SqlWhereBuilder<T> DatePartBetween(Expression<Func<T, object>> selector, string part, int from, int to)
    {
        var col = GetName(selector);
        var fromParam = AddParam(from);
        var toParam = AddParam(to);
        return AddCondition(d =>
            $"{d.DatePartExpression(d.QuoteIdentifier(col), part)} BETWEEN {d.ParameterPrefix}{fromParam} AND {d.ParameterPrefix}{toParam}");
    }

    // ── CAST comparison ───────────────────────────────────────────────────────

    /// <summary>Adds <c>CAST([col] AS typeName) = @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> CastEqualTo<TValue>(
        Expression<Func<T, object>> selector, string castType, TValue value)
        => AddCastComparison(selector, castType, "=", value);

    /// <summary>Adds <c>CAST([col] AS typeName) > @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> CastGreaterThan<TValue>(
        Expression<Func<T, object>> selector, string castType, TValue value)
        => AddCastComparison(selector, castType, ">", value);

    /// <summary>Adds <c>CAST([col] AS typeName) &lt; @pw{n}</c>.</summary>
    public SqlWhereBuilder<T> CastLessThan<TValue>(
        Expression<Func<T, object>> selector, string castType, TValue value)
        => AddCastComparison(selector, castType, "<", value);

    // ── IS DISTINCT FROM ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds IS DISTINCT FROM check (NULL-safe not-equal).
    /// Uses dialect-native syntax or fallback.
    /// </summary>
    public SqlWhereBuilder<T> IsDistinctFrom<TValue>(
        Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d =>
            d.IsDistinctFromExpression(d.QuoteIdentifier(col), $"{d.ParameterPrefix}{param}"));
    }

    /// <summary>
    /// Adds IS NOT DISTINCT FROM check (NULL-safe equal).
    /// Uses dialect-native syntax or fallback.
    /// </summary>
    public SqlWhereBuilder<T> IsNotDistinctFrom<TValue>(
        Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d =>
            d.IsNotDistinctFromExpression(d.QuoteIdentifier(col), $"{d.ParameterPrefix}{param}"));
    }

    // ── COALESCE comparison ───────────────────────────────────────────────────

    /// <summary>
    /// Adds <c>COALESCE([col], fallbackSql) = @pw{n}</c>.
    /// Use this to treat NULLs as a specific value in a comparison.
    /// Example: <c>.CoalesceEqualTo(x => x.Department, "'N/A'", "N/A")</c>
    /// → <c>COALESCE([Department], 'N/A') = @pw0</c>
    /// </summary>
    public SqlWhereBuilder<T> CoalesceEqualTo<TValue>(
        Expression<Func<T, TValue>> selector,
        string fallbackSql,
        TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d =>
            $"{d.CoalesceExpression(d.QuoteIdentifier(col), fallbackSql)} = {d.ParameterPrefix}{param}");
    }

    // ── Sub-group ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a nested group of conditions wrapped in parentheses, sharing the parent's parameter store.
    /// </summary>
    /// <example>
    /// <code>
    /// builder
    ///     .EqualTo(x => x.Status, 1)
    ///     .Or()
    ///     .AddSubGroup(g => g
    ///         .GreaterThan(x => x.Age, 18)
    ///         .LessThan(x => x.Age, 65));
    /// // SQL: ([Status] = @pw0 OR ([Age] > @pw1 AND [Age] &lt; @pw2))
    /// </code>
    /// </example>
    public SqlWhereBuilder<T> AddSubGroup(Action<SqlWhereBuilder<T>> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var sub = new SqlWhereBuilder<T>(_state);
        configure(sub);
        return AddCondition(d => $"({sub.BuildSql(d)})");
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private SqlWhereBuilder<T> AddCastComparison<TValue>(
        Expression<Func<T, object>> selector, string castType, string op, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d =>
            $"{d.CastExpression(d.QuoteIdentifier(col), castType)} {op} {d.ParameterPrefix}{param}");
    }

    private SqlWhereBuilder<T> AddDatePartComparison(Expression<Func<T, object>> selector, string part, string op, int value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d =>
            $"{d.DatePartExpression(d.QuoteIdentifier(col), part)} {op} {d.ParameterPrefix}{param}");
    }

    /// <summary>
    /// Escapes special LIKE wildcard characters in a raw string value so they are treated as literals.
    /// Replaces <c>%</c> with <c>\%</c> and <c>_</c> with <c>\_</c>.
    /// </summary>
    private static string EscapeLikePattern(string value)
        => value.Replace("%", "\\%").Replace("_", "\\_");
}
