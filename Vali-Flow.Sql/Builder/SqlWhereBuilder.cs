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
/// // SQL: (([Price] > @pw0 AND [Price] &lt; @pw1) OR ([Category] = @pw2))
/// </code>
/// </example>
public sealed class SqlWhereBuilder<T> where T : class
{
    // Shared state allows sub-groups (AddSubGroup) to share the same parameter counter
    // and dictionary as the root builder, preventing parameter name collisions.
    private sealed class SharedState
    {
        public int ParamIndex;
        public readonly Dictionary<string, object> Parameters = new();
    }

    private readonly SharedState _state;
    private readonly List<(Func<ISqlDialect, string> SqlFactory, bool IsAnd)> _conditions = new();
    private bool _nextIsAnd = true;

    /// <summary>Creates a new root WHERE builder.</summary>
    public SqlWhereBuilder() => _state = new SharedState();

    private SqlWhereBuilder(SharedState state) => _state = state;

    // ── Logic ─────────────────────────────────────────────────────────────────

    /// <summary>The next condition will be ANDed with the previous group (default behavior).</summary>
    public SqlWhereBuilder<T> And()
    {
        _nextIsAnd = true;
        return this;
    }

    /// <summary>The next condition will start a new OR group.</summary>
    public SqlWhereBuilder<T> Or()
    {
        _nextIsAnd = false;
        return this;
    }

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
        var param = AddParam($"%{value}%");
        return AddCondition(d => $"{d.QuoteIdentifier(col)} {d.LikeOperator} {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] NOT LIKE '%value%'</c>.</summary>
    public SqlWhereBuilder<T> NotContains(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"%{value}%");
        return AddCondition(d => $"{d.QuoteIdentifier(col)} NOT {d.LikeOperator} {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] LIKE 'value%'</c>.</summary>
    public SqlWhereBuilder<T> StartsWith(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"{value}%");
        return AddCondition(d => $"{d.QuoteIdentifier(col)} {d.LikeOperator} {d.ParameterPrefix}{param}");
    }

    /// <summary>Adds <c>[col] LIKE '%value'</c>.</summary>
    public SqlWhereBuilder<T> EndsWith(Expression<Func<T, string?>> selector, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var col = GetName(selector);
        var param = AddParam($"%{value}");
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

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the SQL WHERE fragment and returns it with its collected parameters.
    /// Returns an empty string when no conditions have been added.
    /// </summary>
    internal (string Sql, IReadOnlyDictionary<string, object> Parameters) Build(ISqlDialect dialect)
        => (BuildSql(dialect), _state.Parameters);

    // ── Internals ─────────────────────────────────────────────────────────────

    private string BuildSql(ISqlDialect dialect)
    {
        if (_conditions.Count == 0) return string.Empty;

        // Group conditions: a new group begins when isAnd=false or when there is no current group.
        // AND joins conditions within a group; OR joins groups together.
        var groups = new List<List<string>>();
        List<string>? current = null;

        foreach (var (factory, isAnd) in _conditions)
        {
            string sql = factory(dialect);
            if (!isAnd || current == null)
            {
                current = new List<string>();
                groups.Add(current);
            }

            current.Add(sql);
        }

        var groupSqls = groups.Select(g =>
            g.Count == 1 ? g[0] : $"({string.Join(" AND ", g)})");

        string result = string.Join(" OR ", groupSqls);
        return groups.Count > 1 ? $"({result})" : result;
    }

    private SqlWhereBuilder<T> AddCondition(Func<ISqlDialect, string> factory)
    {
        _conditions.Add((factory, _nextIsAnd));
        _nextIsAnd = true;
        return this;
    }

    private string AddParam(object? value)
    {
        string name = $"pw{_state.ParamIndex++}";
        _state.Parameters[name] = value ?? DBNull.Value;
        return name;
    }

    private static string GetName<TValue>(Expression<Func<T, TValue>> selector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return ExpressionHelper.GetMemberName(selector);
    }
}
