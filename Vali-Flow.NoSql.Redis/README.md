# Vali-Flow.NoSql.Redis

Translates a `ValiFlow<T>` fluent filter into a RediSearch query string — ready to pass to `db.FT().Search(indexName, new Query(result))`.

## Install

```bash
dotnet add package Vali-Flow.NoSql.Redis
```

## Quick Start

```csharp
using NRedisStack;
using NRedisStack.Search;
using StackExchange.Redis;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Redis.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .In(x => x.Category, new[] { "Electronics", "Computers" })
    .GreaterThanOrEqualTo(x => x.Price, 50m)
    .StartsWith(x => x.Name, "Pro");

string query = filter.ToRedisSearch();

IDatabase db = redis.GetDatabase();
SearchResult results = db.FT().Search("idx:products", new Query(query));
```

## What it supports

| Operation | RediSearch output |
|-----------|------------------|
| `EqualTo` (numeric/bool) | `@field:[v v]` |
| `EqualTo` (string) | `@field:{"v"}` |
| `NotEqualTo` (numeric) | `(-@field:[v v])` |
| `NotEqualTo` (string) | `-@field:{"v"}` |
| `GreaterThan` / `GreaterThanOrEqualTo` | `@field:[(v +inf]` / `@field:[v +inf]` |
| `LessThan` / `LessThanOrEqualTo` | `@field:[-inf (v]` / `@field:[-inf v]` |
| `Contains` / `StartsWith` / `EndsWith` | `@field:*txt*` / `@field:txt*` / `@field:*txt` |
| `In` (numeric) | `(@field:[v1 v1]\|@field:[v2 v2]\|…)` |
| `In` (string) | `@field:{"v1"\|"v2"\|…}` |
| `And` / `Or` / `Not` | `(a b)` / `(a \| b)` / `-(a)` |

## Limitations

| Limitation | Detail |
|-----------|--------|
| `IsNull` / `IsNotNull` | Throws `NotSupportedException`. RediSearch has no native field-existence query. Handle null checks at the application level. |
| Empty `In` list | Produces `(-*)` — always false. |
| Mixed numeric/string `In` | Throws `InvalidOperationException`. All non-null values in a single `In` call must be of the same kind. |
| DIALECT 2 | Required for quoted tag values. NRedisStack 1.3.0+ appends it automatically. |

## Custom value converter

Pass a `customConverter` delegate to control how domain types appear in the query string:

```csharp
// Price stored as integer cents in Redis
record Price(int Cents);

string query = new ValiFlow<Product>()
    .GreaterThanOrEqualTo(x => x.UnitPrice, new Price(1000))
    .ToRedisSearch(value =>
    {
        if (value is Price p) return p.Cents.ToString(CultureInfo.InvariantCulture);
        return null;
    });

// query → "@UnitPrice:[1000 +inf]"
```

## Expression overload

```csharp
Expression<Func<Order, bool>> expr = o => o.Status == "Pending" && o.Total >= 200m;
string query = expr.ToRedisSearch();
```

## Notes

- Depends on `NRedisStack` for the `Query` type only — no connection or execution logic.
- Field names mirror .NET property names. Use a naming convention in your RediSearch index definition to map to custom field names.
- `Contains`, `StartsWith`, and `EndsWith` target TEXT fields in your RediSearch schema.

## Full documentation

[docs/en/vali-flow-nosql-redis.md](https://github.com/your-org/Vali-Flow/blob/main/docs/en/vali-flow-nosql-redis.md)
