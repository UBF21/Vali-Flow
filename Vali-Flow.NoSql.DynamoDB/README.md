# Vali-Flow.NoSql.DynamoDB

Translates a `ValiFlow<T>` fluent filter into a DynamoDB `FilterExpression` string with the matching `ExpressionAttributeNames` and `ExpressionAttributeValues` — ready to apply to a `ScanRequest` or `QueryRequest`.

## Install

```bash
dotnet add package Vali-Flow.NoSql.DynamoDB
```

## Quick Start

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.DynamoDB.Extensions;
using Vali_Flow.NoSql.DynamoDB.Models;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m)
    .In(x => x.RegionCode, new[] { "US", "CA", "MX" })
    .IsNotNull(x => x.CustomerId);

DynamoFilterExpression f = filter.ToDynamoDB();

var request = new ScanRequest
{
    TableName                 = "Orders",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};

ScanResponse response = await client.ScanAsync(request);
```

## What it supports

| Operation | DynamoDB FilterExpression |
|-----------|--------------------------|
| `EqualTo` / `NotEqualTo` | `#f = :v` / `#f <> :v` |
| `GreaterThan` / `GreaterThanOrEqualTo` | `#f > :v` / `#f >= :v` |
| `LessThan` / `LessThanOrEqualTo` | `#f < :v` / `#f <= :v` |
| `Contains` | `contains(#f, :v)` |
| `StartsWith` | `begins_with(#f, :v)` |
| `In` (max 100 values) | `#f IN (:v0, :v1, …)` |
| `IsNull` / `IsNotNull` | `attribute_not_exists(#f)` / `attribute_exists(#f)` |
| `And` / `Or` / `Not` | `(a AND b)` / `(a OR b)` / `NOT (a)` |

## DynamoFilterExpression

The returned object exposes three properties:

| Property | Type | Description |
|----------|------|-------------|
| `FilterExpression` | `string` | The expression string with `#f0`, `:v0` placeholders. |
| `ExpressionAttributeNames` | `IReadOnlyDictionary<string, string>` | Placeholder → attribute name. |
| `ExpressionAttributeValues` | `IReadOnlyDictionary<string, AttributeValue>` | Placeholder → value. |

Call `.ToDictionary()` on each property when assigning to `ScanRequest` or `QueryRequest`.

## Limitations

| Limitation | Detail |
|-----------|--------|
| `EndsWith` | Throws `NotSupportedException`. DynamoDB has no trailing-wildcard function. Use `Contains` or apply the check client-side. |
| `In` list > 100 values | Throws `InvalidOperationException`. Split the query or batch the values. |
| Empty `In` list | Produces `(attribute_exists(#f) AND attribute_not_exists(#f))` — always false. |

## Custom value converter

Pass a `customConverter` delegate to handle domain types not in the built-in switch:

```csharp
// Store a DateTimeOffset as ISO-8601 string in DynamoDB
DynamoFilterExpression f = new ValiFlow<Event>()
    .GreaterThan(x => x.StartsAt, DateTimeOffset.UtcNow)
    .ToDynamoDB(value =>
    {
        if (value is DateTimeOffset dto)
            return new AttributeValue { S = dto.ToString("O") };
        return null;
    });
```

## Expression overload

```csharp
Expression<Func<User, bool>> expr = u => u.IsActive && u.Age >= 21;
DynamoFilterExpression f = expr.ToDynamoDB();
```

## Notes

- Depends on `AWSSDK.DynamoDBv2` only — no connection or execution logic.
- All attribute name placeholders (`#f0`, `#f1`, …) are auto-generated, preventing conflicts with DynamoDB reserved words.
- Field names mirror .NET property names.

## Contributing

Contributions, issues, and feature requests are welcome. Feel free to open a pull request or an issue on [GitHub](https://github.com/UBF21/vali-flow).

If this package is useful to you, consider supporting its development:

- **Latin America** — [MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **International** — [PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)

## License

Licensed under the [MIT License](LICENSE).  
Copyright &copy; 2025 Felipe Rafael Montenegro Morriberon. All rights reserved.

## Full documentation

[vali-flow-docs.netlify.app/docs/adapters/nosql/dynamodb/overview](https://vali-flow-docs.netlify.app/docs/adapters/nosql/dynamodb/overview)
