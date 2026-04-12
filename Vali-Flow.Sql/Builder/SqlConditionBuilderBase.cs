using System.Linq.Expressions;
using Vali_Flow.Sql.Dialects;

namespace Vali_Flow.Sql.Builder;

/// <summary>
/// Abstract base for SQL condition builders (WHERE, HAVING).
/// Encapsulates the shared AND/OR grouping algorithm, parameter store, and condition list.
/// Concrete builders supply their own parameter prefix and public condition methods.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type (CRTP — enables fluent return of the correct type).</typeparam>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class SqlConditionBuilderBase<TBuilder, T>
    where TBuilder : SqlConditionBuilderBase<TBuilder, T>
    where T : class
{
    /// <summary>
    /// Shared parameter counter and dictionary.
    /// Sub-builders (e.g. AddSubGroup) receive the parent's state so parameter names never collide.
    /// </summary>
    /// <summary>
    /// Shared parameter counter and state for grouped conditions.
    /// </summary>
    protected sealed class SharedState
    {
        /// <summary>Current parameter index for generating unique parameter names.</summary>
        internal int ParamIndex;
        /// <summary>Dictionary of parameter names and values.</summary>
        public readonly Dictionary<string, object> Parameters = new();
    }

    private readonly List<(Func<ISqlDialect, string> SqlFactory, bool IsAnd)> _conditions = new();
    private bool _nextIsAnd = true;
    /// <summary>Shared state for parameter management across builder hierarchy.</summary>
    protected readonly SharedState _state;

    /// <summary>Parameter name prefix, e.g. "pw" for WHERE or "ph" for HAVING.</summary>
    protected abstract string ParamPrefix { get; }

    /// <summary>Creates a root builder with its own parameter store.</summary>
    protected SqlConditionBuilderBase() => _state = new SharedState();

    /// <summary>Creates a child builder that shares a parent's parameter store (used for sub-groups).</summary>
    protected SqlConditionBuilderBase(SharedState state) => _state = state;

    // ── Logic ─────────────────────────────────────────────────────────────────

    /// <summary>The next condition will be ANDed with the previous group (default behavior).</summary>
    public TBuilder And()
    {
        _nextIsAnd = true;
        return (TBuilder)this;
    }

    /// <summary>The next condition will start a new OR group.</summary>
    public TBuilder Or()
    {
        _nextIsAnd = false;
        return (TBuilder)this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the SQL clause fragment and returns it with its collected parameters.
    /// Returns an empty string when no conditions have been added.
    /// </summary>
    internal (string Sql, IReadOnlyDictionary<string, object> Parameters) Build(ISqlDialect dialect)
        => (BuildSql(dialect), _state.Parameters);

    // ── Protected helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds a condition factory that will be evaluated when building the final SQL.
    /// </summary>
    /// <param name="factory">Function that generates SQL based on the dialect.</param>
    /// <returns>The builder instance for method chaining.</returns>
    protected TBuilder AddCondition(Func<ISqlDialect, string> factory)
    {
        _conditions.Add((factory, _nextIsAnd));
        _nextIsAnd = true;
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a parameter to the shared parameter dictionary and returns its name.
    /// </summary>
    /// <param name="value">The parameter value to store.</param>
    /// <returns>The generated parameter name.</returns>
    protected string AddParam(object? value)
    {
        string name = $"{ParamPrefix}{_state.ParamIndex++}";
        _state.Parameters[name] = value ?? DBNull.Value;
        return name;
    }

    /// <summary>
    /// Extracts the property name from a member expression.
    /// </summary>
    /// <typeparam name="TValue">The type of the property.</typeparam>
    /// <param name="selector">Expression selecting the property.</param>
    protected static string GetName<TValue>(Expression<Func<T, TValue>> selector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return ExpressionHelper.GetMemberName(selector);
    }

    // ── AND/OR grouping algorithm ─────────────────────────────────────────────

    /// <summary>
    /// Groups conditions by OR boundaries: each <c>Or()</c> call starts a new group;
    /// conditions within a group are joined with AND; groups are joined with OR.
    /// </summary>
    /// <param name="dialect">The SQL dialect to use for rendering conditions.</param>
    /// <param name="wrapMultipleGroups">
    /// When <c>true</c> (default) wraps the result in parentheses if there are multiple OR groups.
    /// Pass <c>false</c> when the caller (e.g. <see cref="SqlWhereBuilder{T}.AddSubGroup"/>) will
    /// add its own outer parentheses, to avoid double-wrapping like <c>((...AND...))</c>.
    /// </param>
    internal string BuildSql(ISqlDialect dialect, bool wrapMultipleGroups = true)
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

        bool multipleGroups = groups.Count > 1;

        var groupSqls = groups.Select(g =>
            g.Count == 1 ? g[0] :
            multipleGroups ? $"({string.Join(" AND ", g)})" :
            string.Join(" AND ", g));

        string result = string.Join(" OR ", groupSqls);
        return multipleGroups && wrapMultipleGroups ? $"({result})" : result;
    }
}
