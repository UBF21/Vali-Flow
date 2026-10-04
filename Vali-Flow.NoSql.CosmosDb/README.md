# Vali-Flow.NoSql.CosmosDb

Azure Cosmos DB SQL API WHERE-clause builder that translates `ValiFlow<T>` filters into native `CosmosFilterExpression` objects.

See the [main README](../../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.NoSql.CosmosDb
```

## Quick Example

```csharp
using Vali_Flow.NoSql.CosmosDb.Extensions;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

CosmosFilterExpression f = filter.ToCosmosDb();

var queryText = $"SELECT * FROM c WHERE {f.WhereClause}";
var query = new QueryDefinition(queryText);
foreach (var kv in f.Parameters) query = query.WithParameter(kv.Key, kv.Value);

using var iterator = container.GetItemQueryIterator<Order>(query);
```

## Features

- Translates `ValiFlow<T>` expressions to a Cosmos SQL API WHERE clause fragment
- Native functions: `CONTAINS`, `STARTSWITH`, `ENDSWITH`, `IS_NULL`
- Parameter placeholders (`@p0`, `@p1`, …), one per value — no attribute-name placeholders needed
- No dependency on the `Microsoft.Azure.Cosmos` SDK — pure string/parameter builder

## Limitations

- Does not depend on the Cosmos SDK — it only produces the WHERE fragment. You build your own `QueryDefinition` / `Container.GetItemQueryIterator` with it.
- `In` with an empty list throws `InvalidOperationException` — filter the empty case before building the query.
- No upper bound is enforced on `In` list size by this package; very large lists may still hit Cosmos DB's own query size/complexity limits.

## License

MIT
