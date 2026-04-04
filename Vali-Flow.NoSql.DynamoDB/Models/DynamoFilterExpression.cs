using Amazon.DynamoDBv2.Model;

namespace Vali_Flow.NoSql.DynamoDB.Models;

/// <summary>
/// Encapsulates the result of translating a Vali-Flow filter into a DynamoDB FilterExpression.
/// </summary>
/// <remarks>
/// Pass the three properties directly to a <c>ScanRequest</c> or <c>QueryRequest</c>:
/// <code>
/// var dynamo = filter.ToDynamoDB();
/// var request = new ScanRequest
/// {
///     TableName              = "Orders",
///     FilterExpression       = dynamo.FilterExpression,
///     ExpressionAttributeNames  = dynamo.ExpressionAttributeNames.ToDictionary(),
///     ExpressionAttributeValues = dynamo.ExpressionAttributeValues.ToDictionary()
/// };
/// </code>
/// </remarks>
public sealed class DynamoFilterExpression
{
    /// <summary>The FilterExpression string (e.g. <c>"#f0 = :v0 AND #f1 &gt; :v1"</c>).</summary>
    public string FilterExpression { get; }

    /// <summary>Mapping from placeholder names (<c>#f0</c>) to actual attribute names.</summary>
    public IReadOnlyDictionary<string, string> ExpressionAttributeNames { get; }

    /// <summary>Mapping from value placeholders (<c>:v0</c>) to <see cref="AttributeValue"/> instances.</summary>
    public IReadOnlyDictionary<string, AttributeValue> ExpressionAttributeValues { get; }

    internal DynamoFilterExpression(
        string filterExpression,
        Dictionary<string, string> names,
        Dictionary<string, AttributeValue> values)
    {
        FilterExpression         = filterExpression;
        ExpressionAttributeNames  = names.AsReadOnly();
        ExpressionAttributeValues = values.AsReadOnly();
    }
}
