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
public sealed class SqlHavingBuilder<T> : SqlConditionBuilderBase<SqlHavingBuilder<T>, T>
    where T : class
{
    protected override string ParamPrefix => "ph";

    /// <summary>Creates a new HAVING builder.</summary>
    public SqlHavingBuilder() { }

    // ── COUNT(*) ──────────────────────────────────────────────────────────────

    /// <summary>Adds <c>COUNT(*) = @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountEquals(int value) => AddCountComparison("=", value);

    /// <summary>Adds <c>COUNT(*) &lt;&gt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountNotEquals(int value)
    {
        var param = AddParam(value);
        return AddCondition(d => $"COUNT(*) {d.NotEqualOperator} {d.ParameterPrefix}{param}");
    }

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

    /// <summary>Adds <c>COUNT(*) NOT BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> CountNotBetween(int from, int to)
    {
        var fromParam = AddParam(from);
        var toParam = AddParam(to);
        return AddCondition(d =>
            $"COUNT(*) NOT BETWEEN {d.ParameterPrefix}{fromParam} AND {d.ParameterPrefix}{toParam}");
    }

    // ── SUM ───────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>SUM([col]) = @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("SUM", selector, "=", value);

    /// <summary>Adds <c>SUM([col]) &lt;&gt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumNotEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateNotEqualsComparison("SUM", selector, value);

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

    /// <summary>Adds <c>SUM([col]) NOT BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> SumNotBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
        => AddAggregateNotBetween("SUM", selector, from, to);

    // ── AVG ───────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>AVG([col]) = @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> AverageEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("AVG", selector, "=", value);

    /// <summary>Adds <c>AVG([col]) &lt;&gt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> AverageNotEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateNotEqualsComparison("AVG", selector, value);

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

    /// <summary>Adds <c>AVG([col]) NOT BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> AverageNotBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
        => AddAggregateNotBetween("AVG", selector, from, to);

    // ── MIN ───────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>MIN([col]) = @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MinEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MIN", selector, "=", value);

    /// <summary>Adds <c>MIN([col]) &lt;&gt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MinNotEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateNotEqualsComparison("MIN", selector, value);

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

    /// <summary>Adds <c>MIN([col]) BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> MinBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
        => AddAggregateBetween("MIN", selector, from, to);

    /// <summary>Adds <c>MIN([col]) NOT BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> MinNotBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
        => AddAggregateNotBetween("MIN", selector, from, to);

    // ── MAX ───────────────────────────────────────────────────────────────────

    /// <summary>Adds <c>MAX([col]) = @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MaxEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateComparison("MAX", selector, "=", value);

    /// <summary>Adds <c>MAX([col]) &lt;&gt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> MaxNotEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddAggregateNotEqualsComparison("MAX", selector, value);

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

    /// <summary>Adds <c>MAX([col]) BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> MaxBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
        => AddAggregateBetween("MAX", selector, from, to);

    /// <summary>Adds <c>MAX([col]) NOT BETWEEN @ph{n} AND @ph{n+1}</c>.</summary>
    public SqlHavingBuilder<T> MaxNotBetween<TValue>(Expression<Func<T, TValue>> selector, TValue from, TValue to)
        => AddAggregateNotBetween("MAX", selector, from, to);

    // ── COUNT(DISTINCT) ───────────────────────────────────────────────────────

    /// <summary>Adds <c>COUNT(DISTINCT [col]) > @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountDistinctGreaterThan<TValue>(Expression<Func<T, TValue>> selector, int value)
        => AddCountDistinctComparison(selector, ">", value);

    /// <summary>Adds <c>COUNT(DISTINCT [col]) >= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountDistinctGreaterThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, int value)
        => AddCountDistinctComparison(selector, ">=", value);

    /// <summary>Adds <c>COUNT(DISTINCT [col]) &lt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountDistinctLessThan<TValue>(Expression<Func<T, TValue>> selector, int value)
        => AddCountDistinctComparison(selector, "<", value);

    /// <summary>Adds <c>COUNT(DISTINCT [col]) &lt;= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountDistinctLessThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, int value)
        => AddCountDistinctComparison(selector, "<=", value);

    /// <summary>Adds <c>COUNT(DISTINCT [col]) = @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> CountDistinctEquals<TValue>(Expression<Func<T, TValue>> selector, int value)
        => AddCountDistinctComparison(selector, "=", value);

    // ── SUM(DISTINCT) ─────────────────────────────────────────────────────────

    /// <summary>Adds <c>SUM(DISTINCT [col]) > @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumDistinctGreaterThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddDistinctAggregateComparison("SUM", selector, ">", value);

    /// <summary>Adds <c>SUM(DISTINCT [col]) >= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumDistinctGreaterThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddDistinctAggregateComparison("SUM", selector, ">=", value);

    /// <summary>Adds <c>SUM(DISTINCT [col]) &lt; @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumDistinctLessThan<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddDistinctAggregateComparison("SUM", selector, "<", value);

    /// <summary>Adds <c>SUM(DISTINCT [col]) &lt;= @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumDistinctLessThanOrEqualTo<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddDistinctAggregateComparison("SUM", selector, "<=", value);

    /// <summary>Adds <c>SUM(DISTINCT [col]) = @ph{n}</c>.</summary>
    public SqlHavingBuilder<T> SumDistinctEquals<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        => AddDistinctAggregateComparison("SUM", selector, "=", value);

    // ── Internals ─────────────────────────────────────────────────────────────

    private SqlHavingBuilder<T> AddDistinctAggregateComparison<TValue>(
        string function, Expression<Func<T, TValue>> selector, string op, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{function}(DISTINCT {d.QuoteIdentifier(col)}) {op} {d.ParameterPrefix}{param}");
    }

    private SqlHavingBuilder<T> AddCountDistinctComparison<TValue>(
        Expression<Func<T, TValue>> selector, string op, int value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"COUNT(DISTINCT {d.QuoteIdentifier(col)}) {op} {d.ParameterPrefix}{param}");
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

    private SqlHavingBuilder<T> AddAggregateNotEqualsComparison<TValue>(
        string function, Expression<Func<T, TValue>> selector, TValue value)
    {
        var col = GetName(selector);
        var param = AddParam(value);
        return AddCondition(d => $"{function}({d.QuoteIdentifier(col)}) {d.NotEqualOperator} {d.ParameterPrefix}{param}");
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

    private SqlHavingBuilder<T> AddAggregateNotBetween<TValue>(
        string function, Expression<Func<T, TValue>> selector, TValue from, TValue to)
    {
        var col = GetName(selector);
        var fromParam = AddParam(from);
        var toParam = AddParam(to);
        return AddCondition(d =>
            $"{function}({d.QuoteIdentifier(col)}) NOT BETWEEN {d.ParameterPrefix}{fromParam} AND {d.ParameterPrefix}{toParam}");
    }
}
