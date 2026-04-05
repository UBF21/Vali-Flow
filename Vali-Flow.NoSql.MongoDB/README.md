# Vali-Flow.NoSql.MongoDB

Translates a `ValiFlow<T>` fluent filter into a MongoDB `BsonDocument` — ready to pass to `Find`, `CountDocuments`, `DeleteMany`, and any other MongoDB collection method.

## Install

```bash
dotnet add package Vali-Flow.NoSql.MongoDB
```

## Quick Start

```csharp
using MongoDB.Bson;
using MongoDB.Driver;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.MongoDB.Extensions;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m)
    .In(x => x.RegionCode, new[] { "US", "CA", "MX" })
    .IsNotNull(x => x.CustomerId);

BsonDocument mongoFilter = filter.ToMongo();

// FilterDefinition<T> has an implicit conversion from BsonDocument — no cast needed
var orders = await collection.Find(mongoFilter).ToListAsync();
```

## What it supports

| Operation | MongoDB output |
|-----------|---------------|
| `EqualTo` / `NotEqualTo` | `{ field: v }` / `{ field: { $ne: v } }` |
| `GreaterThan` / `GreaterThanOrEqualTo` | `{ field: { $gt/$gte: v } }` |
| `LessThan` / `LessThanOrEqualTo` | `{ field: { $lt/$lte: v } }` |
| `Contains` / `StartsWith` / `EndsWith` | `{ field: { $regex: /…/i } }` |
| `In` | `{ field: { $in: […] } }` |
| `IsNull` / `IsNotNull` | `{ field: null }` / `{ field: { $ne: null } }` |
| `And` / `Or` / `Not` | `$and` / `$or` / `$nor` |

All supported IR node types are translated. No operations throw `NotSupportedException`.

## Custom value converter

Pass a `customConverter` delegate to handle domain types that are not in the built-in switch, or to override the default serialization:

```csharp
// Store a Money value object as BsonDecimal128
BsonDocument filter = new ValiFlow<Product>()
    .GreaterThan(x => x.Price, new Money(500m, "USD"))
    .ToMongo(value =>
    {
        if (value is Money m) return new BsonDecimal128(m.Amount);
        return null; // fall through for all other types
    });
```

## Expression overload

When you already have a compiled predicate:

```csharp
Expression<Func<Product, bool>> expr = p => p.IsActive && p.Price < 200m;
BsonDocument filter = expr.ToMongo();
```

## Notes

- Depends on `MongoDB.Bson` only — not the full MongoDB.Driver.
- Field names mirror .NET property names. Apply `[BsonElement("name")]` on properties to use custom MongoDB field names.
- `Guid` values are stored as `BsonBinaryData` with `GuidRepresentation.Standard`.

## Full documentation

[docs/en/vali-flow-nosql-mongodb.md](https://github.com/your-org/Vali-Flow/blob/main/docs/en/vali-flow-nosql-mongodb.md)
