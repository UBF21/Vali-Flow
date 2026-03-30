using System.Linq.Expressions;
using Vali_Flow.Sql.Dialects;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Fluent builder for a SQL HAVING clause with aggregate-function conditions.
/// Mirrors the AND/OR grouping algorithm of <see cref="SqlWhereBuilder{T}"/>.
/// </summary>
/// <remarks>
/// Parameters use the <c>ph</c> prefix (e.g. <c>@ph0</c>) to avoid collisions with
/// <see cref="SqlWhereBuilder{T}"/> (<c>@pw</c>) and ExpressionToSqlVisitor (<c>@p</c>).
/// </remarks>
/// <example>
/// <code>
/// var having = new SqlHavingBuilder&lt;Order&gt;()
///     .CountGreaterThan(5)
///     .And()
///     .SumGreaterThan(o => o.Amount, 1000m);
///
/// // SQL: COUNT(*) > @ph0 AND SUM([Amount]) > @ph1
/// </code>
/// </example>
public sealed class SqlHavingBuilder<T> where T : class
{
    private sealed class SharedState
    {
        public int ParamIndex;
        public readonly Dictionary<string, object> Parameters = new();
    }

    private readonly SharedState _state;
    private readonly List<(Func<ISqlDialect, string> SqlFactory, bool IsAnd)> _conditions = new();
    private bool _nextIsAnd = true;

    /// <summary>Creates a new HAVING builder.</summary>
    public SqlHavingBuilder() => _state = new SharedState();

    // ── Logic ─────────────────────────────────────────────────────────────────

    /// <summary>The next condition will be ANDed with the previous (default behavior).</summary>
    public SqlHavingBuilder<T> And()
    {
        _nextIsAnd = true;
        return this;
    }

    /// <summary>The next condition will start a new OR group.</summary>
    public SqlHavingBuilder<T> Or()
    {
        _nextIsAnd = false;
        return this;
    }

    // ── COUNT(*) ──────────────────────────────────────────────────────────────

    /// <summary>Adds <c>COUNT(*) = @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountEquals(int value) => AddCountComparison("=", value);

    /// <summary>Adds <c>COUNT(*) != @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountNotEquals(int value) => AddCountComparison("!=", value);

    /// <summary>Adds <c>COUNT(*) > @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountGreaterThan(int value) => AddCountComparison(">", value);

    /// <summary>Adds <c>COUNT(*) >= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountGreaterThanOrEqualTo(int value) => AddCountComparison(">=", value);

    /// <summary>Adds <c>COUNT(*) &lt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountLessThan(int value) => AddCountComparison("<", value);

    /// <summary>Adds <c>COUNT(*) &lt;= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountLessThanOrEqualTo(int value) => AddCountComparison("<=", value);

    /// <summary>Adds <c>COUNT(*) BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> CountBetween(int from, int to)
    {
        var fromParam = AddParam(from);
        var toParam = AddParam(to);
        return AddCondition(d =>
            $"COUNT(*) BETWEEN {d.ParameterPrefix}{fromParam} AND {d.ParameterPrefix}{toParam}");
    }

    // ── SUM ───────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>SUM([col]) > @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumGreaterThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("SUM", selector, ">", value);

    /// <summary>Adds <c>SUM([col]) >= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumGreaterThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("SUM", selector, ">=", value);

    /// <summary>Adds <c>SUM([col]) &lt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumLessThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("SUM", selector, "<", value);

    /// <summary>Adds <c>SUM([col]) &lt;= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumLessThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("SUM", selector, "<=", value);

    /// <summary>Adds <c>SUM([col]) BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> SumBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
        => AddAggregateBetween("SUM", selector, from, to);

    // ── AVG ───────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>AVG([col]) > @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> AverageGreaterThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("AVG", selector, ">", value);

    /// <summary>Adds <c>AVG([col]) >= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> AverageGreaterThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("AVG", selector, ">=", value);

    /// <summary>Adds <c>AVG([col]) &lt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> AverageLessThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("AVG", selector, "<", value);

    /// <summary>Adds <c>AVG([col]) &lt;= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> AverageLessThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("AVG", selector, "<=", value);

    /// <summary>Adds <c>AVG([col]) BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> AverageBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
        => AddAggregateBetween("AVG", selector, from, to);

    // ── MIN ───────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>MIN([col]) > @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MinGreaterThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MIN", selector, ">", value);

    /// <summary>Adds <c>MIN([col]) >= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MinGreaterThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MIN", selector, ">=", value);

    /// <summary>Adds <c>MIN([col]) &lt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MinLessThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MIN", selector, "<", value);

    /// <summary>Adds <c>MIN([col]) &lt;= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MinLessThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MIN", selector, "<=", value);

    // ── MAX ───────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>MAX([col]) > @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MaxGreaterThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MAX", selector, ">", value);

    /// <summary>Adds <c>MAX([col]) >= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MaxGreaterThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MAX", selector, ">=", value);

    /// <summary>Adds <c>MAX([col]) &lt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MaxLessThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MAX", selector, "<", value);

    /// <summary>Adds <c>MAX([col]) &lt;= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MaxLessThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MAX", selector, "<=", value);

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the SQL HAVING fragment and returns it with its collected parameters.
    /// Returns an empty string when no conditions have been added.
    /// </summary>
    internal (string Sql, IReadOnlyDictionary<string, object> Parameters) Build(ISqlDialect dialect)
        => (BuildSql(dialect), _state.Parameters);

    // ── Internals ─────────────────────────────────────────────────────────────

    private string BuildSql(ISqlDialect dialect)
    {
        if (_conditions.Count == 0) return string.Empty;

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

    private SqlHavingBuilder<T> AddCountComparison(string op, int value)
    {
        var param = AddParam(value);
        return AddCondition(d => $"COUNT(*) {op} {d.ParameterPrefix}{param}");
    }

    private SqlHavingBuilder<T> AddAggregateComparison<TValue>(
        string function, Expression<Func<T, TValue>> selector, string op, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{function}({d.QuoteIdentifier(col)}) {op} {d.ParameterPrefix}{param}");
    }

    private SqlHavingBuilder<T> AddAggregateBetween<TValue>(
        string function, Expression<Func<T, TValue>> selector, TValue from, TValue to)
    {
        var col = GetName(selector);
        var fromParam = AddParam(from);
        var toParam = AddParam(to);
        return AddCondition(d =>
            $"{function}({d.QuoteIdentifier(col)}) BETWEEN {d.ParameterPrefix}{fromParam} AND {d.ParameterPrefix}{toParam}");
    }

    private SqlHavingBuilder<T> AddCondition(Func<ISqlDialect, string> factory)
    {
        _conditions.Add((factory, _nextIsAnd));
        _nextIsAnd = true;
        return this;
    }

    private string AddParam(object? value)
    {
        string name = $"ph{_state.ParamIndex++}";
        _state.Parameters[name] = value ?? DBNull.Value;
        return name;
    }

    private static string GetName<TValue>(Expression<Func<T, TValue>> selector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return ExpressionHelper.GetMemberName(selector);
    }
}
