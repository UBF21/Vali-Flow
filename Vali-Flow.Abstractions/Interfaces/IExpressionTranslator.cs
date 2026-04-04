using System.Linq.Expressions;

namespace Vali_Flow.Abstractions.Interfaces;

/// <summary>
/// Translates a boolean lambda expression into a provider-specific filter representation.
/// </summary>
/// <typeparam name="TOutput">
/// The output type produced by the translator.
/// <list type="bullet">
///   <item>SQL provider: <c>SqlResult</c> (parameterized SQL string + parameters dictionary)</item>
///   <item>NoSql future providers: <c>FilterDefinition&lt;T&gt;</c>, <c>QueryContainer</c>, etc.</item>
/// </list>
/// </typeparam>
/// <remarks>
/// Each provider supplies a concrete implementation.
/// The translator instance carries any provider-specific configuration (e.g. SQL dialect)
/// so callers only need to pass the expression.
/// </remarks>
public interface IExpressionTranslator<TOutput>
{
    /// <summary>Translates the body of a boolean lambda expression to the provider's filter format.</summary>
    /// <typeparam name="T">The entity type the expression operates on.</typeparam>
    /// <param name="expression">A boolean lambda expression representing the filter condition.</param>
    /// <returns>A provider-specific filter object.</returns>
    TOutput Translate<T>(Expression<Func<T, bool>> expression);
}
