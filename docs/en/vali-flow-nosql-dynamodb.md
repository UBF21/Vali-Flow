# Vali-Flow.NoSql.DynamoDB — Complete Reference

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Extension Methods](#extension-methods)
5. [DynamoFilterExpression](#dynamofilterexpression)
6. [Supported Operations](#supported-operations)
7. [Type Mapping](#type-mapping)
8. [Custom Value Converter](#custom-value-converter)
9. [Limitations](#limitations)
10. [Full Example](#full-example)

---

## Overview

**Vali-Flow.NoSql.DynamoDB** translates a `ValiFlow<T>` expression tree into a DynamoDB `FilterExpression` string together with the `ExpressionAttributeNames` and `ExpressionAttributeValues` dictionaries required by `ScanRequest` and `QueryRequest`.

The package depends only on `AWSSDK.DynamoDBv2`. It is a pure query builder with no connection or execution concerns.

---

## Installation

```bash
dotnet add package Vali-Flow.NoSql.DynamoDB
```

`Vali-Flow.Core` is included as a transitive dependency.

---

## Quick Start

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.DynamoDB.Extensions;
using Vali_Flow.NoSql.DynamoDB.Models;

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

ScanResponse response = await client.ScanAsync(request);
```

---

## Extension Methods

Both overloads are in `Vali_Flow.NoSql.DynamoDB.Extensions.ValiFlowDynamoExtensions`.

### `ToDynamoDB<T>(this ValiFlow<T> flow, Func<object?, AttributeValue?>? customConverter = null)`

Translates the conditions accumulated in a `ValiFlow<T>` builder into a `DynamoFilterExpression`.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `flow` | `ValiFlow<T>` | Yes | The builder containing the conditions. |
| `customConverter` | `Func<object?, AttributeValue?>?` | No | Hook for mapping CLR types not handled by the built-in switch. Return `null` to fall through to the default conversion. |

**Returns:** `DynamoFilterExpression` — apply to `ScanRequest` or `QueryRequest`.

### `ToDynamoDB<T>(this Expression<Func<T, bool>> expression, Func<object?, AttributeValue?>? customConverter = null)`

Same translation for a pre-built `Expression<Func<T, bool>>`.

```csharp
Expression<Func<User, bool>> expr = u => u.IsActive && u.Age >= 21;
DynamoFilterExpression f = expr.ToDynamoDB();
```

---

## DynamoFilterExpression

`DynamoFilterExpression` is a sealed class that encapsulates the three values DynamoDB needs:

| Property | Type | Description |
|----------|------|-------------|
| `FilterExpression` | `string` | The expression string, e.g. `"(#f0 = :v0 AND #f1 > :v1)"`. |
| `ExpressionAttributeNames` | `IReadOnlyDictionary<string, string>` | Maps placeholders (`#f0`, `#f1`, …) to actual attribute names. |
| `ExpressionAttributeValues` | `IReadOnlyDictionary<string, AttributeValue>` | Maps placeholders (`:v0`, `:v1`, …) to `AttributeValue` instances. |

The translator generates sequential placeholders (`#f0`, `#f1`, … for names; `:v0`, `:v1`, … for values) to avoid collisions and to sidestep DynamoDB reserved word conflicts.

`IReadOnlyDictionary<K,V>` does not implement `IDictionary<K,V>` directly. Call `.ToDictionary()` when the SDK requires `Dictionary<string, string>` or `Dictionary<string, AttributeValue>`.

---

## Supported Operations

| ValiFlow method | IR node | DynamoDB FilterExpression |
|----------------|---------|---------------------------|
| `.EqualTo(x => x.Field, v)` | `EqualNode(IsNegated: false)` | `#f0 = :v0` |
| `.NotEqualTo(x => x.Field, v)` | `EqualNode(IsNegated: true)` | `#f0 <> :v0` |
| `.GreaterThan(x => x.Field, v)` | `ComparisonNode(GT)` | `#f0 > :v0` |
| `.GreaterThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(GTE)` | `#f0 >= :v0` |
| `.LessThan(x => x.Field, v)` | `ComparisonNode(LT)` | `#f0 < :v0` |
| `.LessThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(LTE)` | `#f0 <= :v0` |
| `.Contains(x => x.Field, "txt")` | `LikeNode(Contains)` | `contains(#f0, :v0)` |
| `.StartsWith(x => x.Field, "pre")` | `LikeNode(StartsWith)` | `begins_with(#f0, :v0)` |
| `.EndsWith(x => x.Field, "suf")` | `LikeNode(EndsWith)` | throws `NotSupportedException` |
| `.In(x => x.Field, list)` | `InNode` | `#f0 IN (:v0, :v1, …)` |
| `.IsNull(x => x.Field)` | `NullNode(IsNull)` | `attribute_not_exists(#f0)` |
| `.IsNotNull(x => x.Field)` | `NullNode(IsNotNull)` | `attribute_exists(#f0)` |
| `.And(a, b)` | `AndNode` | `(a AND b)` |
| `.Or(a, b)` | `OrNode` | `(a OR b)` |
| `.Not(inner)` | `NotNode` | `NOT (inner)` |

---

## Type Mapping

The built-in `ToAttributeValue` switch handles these CLR types:

| CLR type | `AttributeValue` produced |
|----------|---------------------------|
| `null` | `{ NULL = true }` |
| `bool` | `{ BOOL = b }` |
| `string` | `{ S = s }` |
| `int` | `{ N = "value" }` |
| `long` | `{ N = "value" }` |
| `double` | `{ N = "value" }` (InvariantCulture) |
| `float` | `{ N = "value" }` (InvariantCulture) |
| `decimal` | `{ N = "value" }` (InvariantCulture) |
| `Guid` | `{ S = g.ToString() }` |
| `Enum` | `{ N = "underlying_int64" }` |
| anything else | `{ S = v.ToString() }` |

---

## Custom Value Converter

Use `customConverter` when your domain types are not in the table above or when you need different `AttributeValue` representation than the default.

The converter is called **before** the built-in switch. Return `null` to let the default handle the value.

```csharp
// Domain type
record Money(decimal Amount, string Currency);

// Converter: store Money as a number (amount only)
DynamoFilterExpression f = new ValiFlow<Order>()
    .GreaterThan(x => x.Total, new Money(500m, "USD"))
    .ToDynamoDB(value =>
    {
        if (value is Money m)
            return new AttributeValue { N = m.Amount.ToString(CultureInfo.InvariantCulture) };
        return null;
    });
```

Storing a `DateTimeOffset` as an ISO-8601 string in DynamoDB:

```csharp
DynamoFilterExpression f = new ValiFlow<Event>()
    .GreaterThan(x => x.StartsAt, DateTimeOffset.UtcNow)
    .ToDynamoDB(value =>
    {
        if (value is DateTimeOffset dto)
            return new AttributeValue { S = dto.ToString("O") };
        return null;
    });
```

---

## Limitations

| Limitation | Detail |
|-----------|--------|
| `EndsWith` | Throws `NotSupportedException`. DynamoDB has no trailing-wildcard function. Use `Contains` or `StartsWith`, or apply the filter client-side after retrieval. |
| `In` list > 100 values | Throws `InvalidOperationException`. DynamoDB's `IN` operator supports at most 100 operands. Split the query or batch the values. |
| Empty `In` list | Produces `(attribute_exists(#f0) AND attribute_not_exists(#f0))` — an always-false expression. No items match. |
| `ToDictionary()` call required | `ExpressionAttributeNames` and `ExpressionAttributeValues` are `IReadOnlyDictionary<K,V>`. The `ScanRequest` / `QueryRequest` constructors require `Dictionary<K,V>`. Call `.ToDictionary()` on each property before assigning. |

---

## Full Example

A realistic filter combining equality, range, membership, and null checks, applied to a DynamoDB `ScanRequest` and `QueryRequest`:

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.DynamoDB.Extensions;
using Vali_Flow.NoSql.DynamoDB.Models;

IAmazonDynamoDB client = new AmazonDynamoDBClient();

// Build the filter
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m)
    .LessThan(x => x.Total, 5000m)
    .In(x => x.RegionCode, new[] { "US", "CA", "MX" })
    .IsNotNull(x => x.CustomerId);

DynamoFilterExpression f = filter.ToDynamoDB();

// f.FilterExpression:
// "(((#f0 = :v0 AND (#f1 >= :v1 AND (#f2 < :v2 AND (#f3 IN (:v3, :v4, :v5) AND attribute_exists(#f4))))))))

// Use in a Scan
var scanRequest = new ScanRequest
{
    TableName                 = "Orders",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};

ScanResponse scanResponse = await client.ScanAsync(scanRequest);

// Use in a Query (when you have a known partition key)
var queryRequest = new QueryRequest
{
    TableName                 = "Orders",
    KeyConditionExpression    = "#pk = :pkval",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = new Dictionary<string, string>(f.ExpressionAttributeNames)
                                { ["#pk"] = "CustomerId" },
    ExpressionAttributeValues = new Dictionary<string, AttributeValue>(f.ExpressionAttributeValues)
                                { [":pkval"] = new AttributeValue { S = "cust-001" } }
};

QueryResponse queryResponse = await client.QueryAsync(queryRequest);
```

### Handling the EndsWith limitation client-side

```csharp
// DynamoDB cannot filter with EndsWith — retrieve broader results and filter in memory
var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .StartsWith(x => x.Sku, "PROD-");   // narrow with StartsWith

DynamoFilterExpression f = filter.ToDynamoDB();

var scanRequest = new ScanRequest
{
    TableName                 = "Products",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};

ScanResponse response = await client.ScanAsync(scanRequest);

// Apply EndsWith client-side
var results = response.Items
    .Where(item => item["Sku"].S.EndsWith("-V2"))
    .ToList();
```
