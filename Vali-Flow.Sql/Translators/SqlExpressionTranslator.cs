using System.Linq.Expressions;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;

namespace Vali_Flow.Sql.Translators;

/// <summary>
/// SQL implementation of <see cref="IExpressionTranslator{TOutput}"/>.
/// Translates a boolean lambda expression into a parameterized SQL WHERE clause
/// using a specific <see cref="ISqlDialect"/>.
/// </summary>
/// <remarks>
/// The dialect is injected at construction time, making each instance dialect-specific.
/// <code>
/// IExpressionTranslator&lt;SqlResult&gt; translator = new SqlExpressionTranslator(new PostgreSqlDialect());
/// SqlResult result = translator.Translate&lt;User&gt;(x =&gt; x.Age &gt; 18 &amp;&amp; x.IsActive);
/// // result.Sql   → "("age" > @p0 AND "is_active" = TRUE)"
/// // result.Parameters → { "p0": 18 }
/// </code>
/// </remarks>
public sealed class SqlExpressionTranslator : IExpressionTranslator<SqlResult>
{
    private readonly ISqlDialect _dialect;

    /// <param name="dialect">The SQL dialect used to generate identifiers, operators, and parameter prefixes.</param>
    public SqlExpressionTranslator(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    /// <inheritdoc />
    public SqlResult Translate<T>(Expression<Func<T, bool>> expression)
        => ExpressionToSqlVisitor.Translate(expression, _dialect);
}
