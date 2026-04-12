# Vali-Flow.NoSql.Redis

Redis (RediSearch) query builder that translates `ValiFlow<T>` filters into native RediSearch query syntax.

See the [main README](../../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.NoSql.Redis
```

## Quick Example

```csharp
using Vali_Flow.NoSql.Redis.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.Category, "Electronics")
    .GreaterThan(x => x.Price, 100m)
    .Contains(x => x.Name, "phone");

string redisQuery = filter.ToRedisSearch();

var results = db.FT().Search("idx:products", new Query(redisQuery));
```

## Features

- Translates `ValiFlow<T>` expressions to RediSearch query syntax
- Numeric range queries: `@field:[min max]`
- Tag queries: `@field:{tag1|tag2}`
- Wildcard pattern matching: `@field:*pattern*`
- Compatible with NRedisStack

## Limitations

- `IsNull` / `IsNotNull` are not supported (RediSearch has no field-existence syntax)
- Handle null checks at the application level or use custom value converters

## License

MIT
