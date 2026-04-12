# Vali-Flow.NoSql.DynamoDB

AWS DynamoDB filter expression builder that translates `ValiFlow<T>` filters into native `DynamoFilterExpression` objects.

See the [main README](../../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.NoSql.DynamoDB
```

## Quick Example

```csharp
using Vali_Flow.NoSql.DynamoDB.Extensions;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

DynamoFilterExpression f = filter.ToDynamoDB();

var request = new ScanRequest
{
    TableName                 = "Orders",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};

var response = await dynamoDb.ScanAsync(request);
```

## Features

- Translates `ValiFlow<T>` expressions to DynamoDB filter expressions
- Native expression syntax: `#f0 = :v0`, `#f0 > :v0`, `begins_with(#f0, :v0)`
- Attribute name placeholders (`#f0`, `#f1`, etc.)
- Attribute value placeholders (`:v0`, `:v1`, etc.)
- Compatible with AWS SDK for .NET

## Limitations

- `EndsWith` is not supported (DynamoDB has no trailing-wildcard function)
- `In` operator supports at most 100 values (DynamoDB SDK limit)

## License

MIT
