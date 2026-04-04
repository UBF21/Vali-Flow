using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.DynamoDB.Models;
using Vali_Flow.NoSql.DynamoDB.Translators;
using Vali_Flow.NoSql.Extensions;

namespace Vali_Flow.NoSql.DynamoDB.Extensions;

/// <summary>
/// Extension methods that add DynamoDB FilterExpression-building capabilities to <see cref="ValiFlow{T}"/>.
/// </summary>
/// <remarks>
/// The returned <see cref="DynamoFilterExpression"/> encapsulates the filter string and
/// attribute dictionaries for use with <c>ScanRequest</c> or <c>QueryRequest</c>.
/// This package depends only on <c>AWSSDK.DynamoDBv2</c> —
/// it is a query builder with no connection or execution concerns.
/// </remarks>
public static class ValiFlowDynamoExtensions
{
    /// <summary>
    /// Translates the conditions built in this <see cref="ValiFlow{T}"/> instance
    /// into a DynamoDB <see cref="DynamoFilterExpression"/>.
    /// </summary>
    /// <typeparam name="T">The entity / document type.</typeparam>
    /// <param name="flow">The ValiFlow builder containing the conditions.</param>
    /// <returns>
    /// A <see cref="DynamoFilterExpression"/> ready to apply to a <c>ScanRequest</c> or <c>QueryRequest</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// var filter = new ValiFlow&lt;Order&gt;()
    ///     .EqualTo(x => x.Status, "Active")
    ///     .GreaterThan(x => x.Total, 100m);
    ///
    /// DynamoFilterExpression f = filter.ToDynamoDB();
    /// var request = new ScanRequest
    /// {
    ///     TableName              = "Orders",
    ///     FilterExpression       = f.FilterExpression,
    ///     ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ///     ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
    /// };
    /// </code>
    /// </example>
    public static DynamoFilterExpression ToDynamoDB<T>(this ValiFlow<T> flow) where T : class
    {
        if (flow == null) throw new ArgumentNullException(nameof(flow));

        return DynamoFilterTranslator.Translate(flow.ToNoSqlIR());
    }

    /// <summary>
    /// Translates a prebuilt <see cref="Expression{TDelegate}"/> into a DynamoDB <see cref="DynamoFilterExpression"/>.
    /// Use this overload when you already have a compiled expression.
    /// </summary>
    public static DynamoFilterExpression ToDynamoDB<T>(this Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        return DynamoFilterTranslator.Translate(expression.ToNoSqlIR());
    }
}
