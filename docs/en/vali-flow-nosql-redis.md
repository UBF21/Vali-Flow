# Vali-Flow.NoSql.Redis — Complete Reference

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

**Vali-Flow.NoSql.Redis** translates a `ValiFlow<T>` expression tree into a RediSearch query string. The output is passed directly to `db.FT().Search(indexName, new Query(result))`.

The package depends only on `NRedisStack` for the query type. It is a pure query builder with no connection or execution concerns.

> NRedisStack 1.3.0 and later automatically appends `DIALECT 2` to search commands. DIALECT 2 is required for quoted tag values (used by string equality and IN queries). If you use an older version of NRedisStack, append `DIALECT 2` manually.

---

## Installation

```bash
dotnet add package Vali-Flow.NoSql.Redis
```

`Vali-Flow.Core` is included as a transitive dependency.

---

## Quick Start

```csharp
using NRedisStack;
using NRedisStack.Search;
using StackExchange.Redis;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Redis.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 50m);

string query = filter.ToRedisSearch();
// query → "(@IsActive:[1 1] @Price:[(50 +inf])"

IDatabase db = redis.GetDatabase();
SearchResult results = db.FT().Search("idx:products", new Query(query));
```

---

## Extension Methods

Both overloads are in `Vali_Flow.NoSql.Redis.Extensions.ValiFlowRedisSearchExtensions`.

### `ToRedisSearch<T>(this ValiFlow<T> flow, Func<object?, string?>? customConverter = null)`

Translates the conditions accumulated in a `ValiFlow<T>` builder into a RediSearch query string.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `flow` | `ValiFlow<T>` | Yes | The builder containing the conditions. |
| `customConverter` | `Func<object?, string?>?` | No | Hook for mapping CLR types to their tag-value string. Return `null` to fall through to the default conversion. |

**Returns:** `string` — pass to `new Query(result)` or use directly in `FT().Search(...)`.

### `ToRedisSearch<T>(this Expression<Func<T, bool>> expression, Func<object?, string?>? customConverter = null)`

Same translation for a pre-built `Expression<Func<T, bool>>`.

```csharp
Expression<Func<Order, bool>> expr = o => o.Status == "Pending";
string query = expr.ToRedisSearch();
```

---

## Supported Operations

| ValiFlow method | IR node | RediSearch output |
|----------------|---------|-------------------|
| `.EqualTo(x => x.NumField, 42)` | `EqualNode` (numeric) | `@NumField:[42 42]` |
| `.EqualTo(x => x.StrField, "v")` | `EqualNode` (string) | `@StrField:{"v"}` |
| `.NotEqualTo(x => x.NumField, 42)` | `EqualNode(IsNegated, numeric)` | `(-@NumField:[42 42])` |
| `.NotEqualTo(x => x.StrField, "v")` | `EqualNode(IsNegated, string)` | `-@StrField:{"v"}` |
| `.GreaterThan(x => x.Field, v)` | `ComparisonNode(GT)` | `@Field:[(v +inf]` |
| `.GreaterThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(GTE)` | `@Field:[v +inf]` |
| `.LessThan(x => x.Field, v)` | `ComparisonNode(LT)` | `@Field:[-inf (v]` |
| `.LessThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(LTE)` | `@Field:[-inf v]` |
| `.Contains(x => x.Field, "txt")` | `LikeNode(Contains)` | `@Field:*txt*` |
| `.StartsWith(x => x.Field, "pre")` | `LikeNode(StartsWith)` | `@Field:pre*` |
| `.EndsWith(x => x.Field, "suf")` | `LikeNode(EndsWith)` | `@Field:*suf` |
| `.In(x => x.Field, [1, 2, 3])` | `InNode` (numeric) | `(@Field:[1 1]\|@Field:[2 2]\|@Field:[3 3])` |
| `.In(x => x.Field, ["a", "b"])` | `InNode` (string) | `@Field:{"a"\|"b"}` |
| `.And(a, b)` | `AndNode` | `(a b)` |
| `.Or(a, b)` | `OrNode` | `(a \| b)` |
| `.Not(inner)` | `NotNode` | `-(inner)` |

**Exclusive range bounds:** RediSearch uses `(value` (opening parenthesis before the number) to express strict inequality. `GreaterThan` emits `[(v +inf]` and `LessThan` emits `[-inf (v]`.

**Tag escaping:** `\` and `"` inside quoted tag values are escaped to `\\` and `\"`.

**Mixed IN list:** If an `InNode` contains a mix of numeric and non-numeric values, `InvalidOperationException` is thrown. All non-null values in an `In` list must be of the same kind (all numeric/bool, or all strings).

---

## Type Mapping

Values are classified as either numeric (mapped to range queries) or string (mapped to tag queries).

**Numeric/bool types** (use range query syntax `@field:[v v]`):

| CLR type | RediSearch representation |
|----------|--------------------------|
| `bool` | `1` (true) or `0` (false) |
| `int` | string representation |
| `long` | string representation |
| `double` | string representation (InvariantCulture) |
| `float` | string representation (InvariantCulture) |
| `decimal` | converted to `double`, then string |

**String/other types** (use tag query syntax `@field:{"value"}`):

| CLR type | Tag value |
|----------|-----------|
| `string` | the string itself |
| `Enum` | `.ToString()` (name, not number) |
| anything else | `.ToString()` |

---

## Custom Value Converter

Use `customConverter` to control how domain types appear in the query string. The converter is called **before** the built-in classification. Return `null` to let the default handle the value.

```csharp
// Domain type
record Money(decimal Amount, string Currency);

// Converter: use the numeric amount so it lands in a range query
string query = new ValiFlow<Product>()
    .GreaterThan(x => x.Price, new Money(100m, "USD"))
    .ToRedisSearch(value =>
    {
        if (value is Money m)
            return m.Amount.ToString(CultureInfo.InvariantCulture);
        return null;
    });

// query → "@Price:[(100 +inf]"
```

Returning a string from the converter always produces a tag query for equality, and is used as-is for range queries (so return an invariant-culture number string for numeric fields).

---

## Limitations

| Limitation | Detail |
|-----------|--------|
| `IsNull` / `IsNotNull` | Throws `NotSupportedException`. RediSearch has no native field-existence query. Handle null checks at the application level or use a sentinel value in your index. |
| Empty `In` list | Produces `(-*)` — a negation of the match-all token, which is always false. |
| Mixed numeric/string `In` | Throws `InvalidOperationException`. All non-null values in a single `In` call must be of the same kind. |
| Pattern fields | `Contains`, `StartsWith`, and `EndsWith` target TEXT fields. The field must be indexed as `TEXT` in your RediSearch schema for prefix/suffix search to work correctly. |
| DIALECT 2 | Quoted tag values require DIALECT 2. NRedisStack 1.3.0+ appends it automatically. Older versions require `new Query(query).Dialect(2)`. |

---

## Full Example

A realistic filter across product data, combined with NRedisStack execution:

```csharp
using NRedisStack;
using NRedisStack.Search;
using StackExchange.Redis;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Redis.Extensions;

// Build the filter
var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .In(x => x.Category, new[] { "Electronics", "Computers" })
    .GreaterThanOrEqualTo(x => x.Price, 50m)
    .LessThan(x => x.Price, 1500m)
    .StartsWith(x => x.Name, "Pro");

string queryString = filter.ToRedisSearch();
// queryString:
// "(@IsActive:[1 1] (@Category:{"Electronics"|"Computers"} (@Price:[50 +inf] (@Price:[-inf (1500] @Name:Pro*))))"

// Execute
ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
IDatabase db = redis.GetDatabase();

SearchResult result = db.FT().Search("idx:products", new Query(queryString)
    .ReturnFields("Name", "Price", "Category")
    .Limit(0, 25));

foreach (Document doc in result.Documents)
{
    Console.WriteLine($"{doc["Name"]} — {doc["Price"]}");
}
```

### Custom converter for a value object

```csharp
// Price stored as cents (integer) in Redis
record Price(int Cents);

string query = new ValiFlow<Product>()
    .GreaterThanOrEqualTo(x => x.UnitPrice, new Price(1000))
    .ToRedisSearch(value =>
    {
        if (value is Price p)
            return p.Cents.ToString(CultureInfo.InvariantCulture);
        return null;
    });

// query → "@UnitPrice:[1000 +inf]"
```
