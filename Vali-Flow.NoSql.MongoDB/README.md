# Vali-Flow.NoSql.MongoDB

MongoDB BSON filter builder that translates `ValiFlow<T>` filters into native `BsonDocument` queries.

See the [main README](../../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.NoSql.MongoDB
```

## Quick Example

```csharp
using Vali_Flow.NoSql.MongoDB.Extensions;

var filter = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Age, 18);

BsonDocument bsonFilter = filter.ToMongo();

var users = await collection.Find(bsonFilter).ToListAsync();
```

## Features

- Translates `ValiFlow<T>` expressions to BSON filters
- Native MongoDB query syntax: `{ $eq, $gt, $in, $and, $or, $nor }`
- String pattern matching: regex for Contains, StartsWith, EndsWith
- Custom value converters for complex types

## License

MIT
