# Vali-Flow.NoSql.Elasticsearch — Complete Reference

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Extension Methods](#extension-methods)
5. [Supported Operations](#supported-operations)
6. [Type Mapping](#type-mapping)
7. [Custom Value Converter](#custom-value-converter)
8. [Limitations](#limitations)
9. [Full Example](#full-example)

---

## Overview

**Vali-Flow.NoSql.Elasticsearch** translates a `ValiFlow<T>` expression tree into an Elasticsearch `Query` object (`Elastic.Clients.Elasticsearch.QueryDsl.Query`). The output plugs directly into any operation that accepts a query — `Search`, `Count`, `DeleteByQuery`, and more.

The package depends only on `Elastic.Clients.Elasticsearch` for query types. It has no connection or execution concerns.

---

## Installation

```bash
dotnet add package Vali-Flow.NoSql.Elasticsearch
```

`Vali-Flow.Core` is included as a transitive dependency.

---

## Quick Start

```csharp
using Elastic.Clients.Elasticsearch;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Elasticsearch.Extensions;

var filter = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Age, 18);

Query esFilter = filter.ToElasticsearch();
var results = await client.SearchAsync<User>(s => s.Query(esFilter));
```

---

## Extension Methods

Both overloads are in `Vali_Flow.NoSql.Elasticsearch.Extensions.ValiFlowElasticsearchExtensions`.

### `ToElasticsearch<T>(this ValiFlow<T> flow, Func<object?, FieldValue?>? customConverter = null)`

Translates the conditions accumulated in a `ValiFlow<T>` builder into an Elasticsearch `Query`.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `flow` | `ValiFlow<T>` | Yes | The builder containing the conditions. |
| `customConverter` | `Func<object?, FieldValue?>?` | No | Hook for mapping CLR types not handled by the built-in switch. Return `null` to fall through to the default conversion. |

**Returns:** `Query` — pass directly to `Search`, `Count`, `DeleteByQuery`, etc.

### `ToElasticsearch<T>(this Expression<Func<T, bool>> expression, Func<object?, FieldValue?>? customConverter = null)`

Same translation for a pre-built `Expression<Func<T, bool>>`.

```csharp
Expression<Func<Product, bool>> expr = p => p.Category == "Electronics" && p.Price < 500m;
Query filter = expr.ToElasticsearch();
```

---

## Supported Operations

| ValiFlow method | IR node | Elasticsearch query |
|----------------|---------|---------------------|
| `.EqualTo(x => x.Field, v)` | `EqualNode(IsNegated: false)` | `term { field: value }` |
| `.NotEqualTo(x => x.Field, v)` | `EqualNode(IsNegated: true)` | `bool { must_not: [term { field: value }] }` |
| `.GreaterThan(x => x.Field, v)` | `ComparisonNode(GT)` | `range { field: { gt: value } }` |
| `.GreaterThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(GTE)` | `range { field: { gte: value } }` |
| `.LessThan(x => x.Field, v)` | `ComparisonNode(LT)` | `range { field: { lt: value } }` |
| `.LessThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(LTE)` | `range { field: { lte: value } }` |
| `.Contains(x => x.Field, "txt")` | `LikeNode(Contains)` | `wildcard { field: "*txt*", case_insensitive: true }` |
| `.StartsWith(x => x.Field, "pre")` | `LikeNode(StartsWith)` | `wildcard { field: "pre*", case_insensitive: true }` |
| `.EndsWith(x => x.Field, "suf")` | `LikeNode(EndsWith)` | `wildcard { field: "*suf", case_insensitive: true }` |
| `.In(x => x.Field, list)` | `InNode` | `terms { field: [...] }` |
| `.IsNull(x => x.Field)` | `NullNode(IsNull)` | `bool { must_not: [exists { field }] }` |
| `.IsNotNull(x => x.Field)` | `NullNode(IsNotNull)` | `exists { field }` |
| `.And(a, b)` | `AndNode` | `bool { must: [a, b] }` |
| `.Or(a, b)` | `OrNode` | `bool { should: [a, b], minimum_should_match: 1 }` |
| `.Not(inner)` | `NotNode` | `bool { must_not: [inner] }` |

Range queries use `NumberRangeQuery`, which requires a numeric field. The value is converted to `double` via `Convert.ToDouble`. Passing a non-numeric value to a range comparison throws `NotSupportedException`.

Wildcard special characters (`*`, `?`, `\`) in pattern strings are escaped before building the Elasticsearch pattern.

---

## Type Mapping

The built-in `ToFieldValue` switch handles these CLR types for term and terms queries:

| CLR type | `FieldValue` produced |
|----------|-----------------------|
| `null` | `FieldValue.Null` |
| `bool` | `FieldValue.Boolean(b)` |
| `int` | `FieldValue.Long(i)` |
| `long` | `FieldValue.Long(l)` |
| `double` | `FieldValue.Double(d)` |
| `float` | `FieldValue.Double(f)` |
| `decimal` | `FieldValue.Double((double)dec)` |
| `string` | `FieldValue.String(s)` |
| `Enum` | `FieldValue.Long(Convert.ToInt64(e))` |
| anything else | `FieldValue.String(v.ToString())` |

---

## Custom Value Converter

Use `customConverter` when your domain types are not in the table above, or when you need a different `FieldValue` representation than the default.

The converter is called **before** the built-in switch. Return `null` to let the default handle the value.

```csharp
// Domain type
record Money(decimal Amount, string Currency);

// Converter: represent Money as a double FieldValue
Query filter = new ValiFlow<Product>()
    .LessThan(x => x.Price, new Money(500m, "USD"))
    .ToElasticsearch(value =>
    {
        if (value is Money m)
            return FieldValue.Double((double)m.Amount);
        return null;
    });
```

Forcing `decimal` to use string representation in a keyword field:

```csharp
Query filter = new ValiFlow<Invoice>()
    .EqualTo(x => x.TaxRate, 0.21m)
    .ToElasticsearch(value =>
    {
        if (value is decimal d)
            return FieldValue.String(d.ToString("G", CultureInfo.InvariantCulture));
        return null;
    });
```

---

## Limitations

Elasticsearch supports all IR node types. There are no operations that throw `NotSupportedException`.

Behavioral notes:

- **IsNull** maps to `bool { must_not: [exists { field }] }`. This matches documents where the field is absent or explicitly `null`, which is standard Elasticsearch behavior.
- **Empty In list** maps to `bool { must_not: [match_all {}] }` — an always-false query. No documents match.
- **Range queries** convert the value to `double`. If the field stores dates, use a `customConverter` to produce a `DateRangeQuery` instead.
- Field names come directly from the .NET property name. To use a custom Elasticsearch field name, configure your index mappings or serializer settings accordingly.

---

## Full Example

A realistic filter combining equality, range, membership, null checks, and logical composition, executed against Elasticsearch:

```csharp
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Elasticsearch.Extensions;

// Build the filter
var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .In(x => x.Category, new[] { "Electronics", "Computers" })
    .GreaterThanOrEqualTo(x => x.Price, 50m)
    .LessThan(x => x.Price, 1500m)
    .IsNotNull(x => x.Sku);

Query esFilter = filter.ToElasticsearch();

// Execute with the client
ElasticsearchClient client = new(new Uri("http://localhost:9200"));

var response = await client.SearchAsync<Product>(s => s
    .Index("products")
    .Query(esFilter)
    .Sort(sort => sort.Field(p => p.Price, f => f.Order(SortOrder.Asc)))
    .From(0)
    .Size(25));

IReadOnlyCollection<Product> products = response.Documents;

// Elasticsearch DSL equivalent:
// {
//   "query": {
//     "bool": {
//       "must": [
//         { "term": { "IsActive": true } },
//         { "bool": { "must": [
//           { "terms": { "Category": ["Electronics", "Computers"] } },
//           { "bool": { "must": [
//             { "range": { "Price": { "gte": 50.0 } } },
//             { "bool": { "must": [
//               { "range": { "Price": { "lt": 1500.0 } } },
//               { "exists": { "field": "Sku" } }
//             ]}}
//           ]}}
//         ]}}
//       ]
//     }
//   }
// }
```

### Using the expression overload

```csharp
Expression<Func<Order, bool>> recentLargeOrders =
    o => o.CreatedAt >= DateTime.UtcNow.AddDays(-7) && o.Total > 1000m;

Query filter = recentLargeOrders.ToElasticsearch();

var response = await client.SearchAsync<Order>(s => s
    .Index("orders")
    .Query(filter));
```
