# Vali-Flow.NoSql.Couchbase

Couchbase N1QL (SQL++) WHERE-clause builder that translates `ValiFlow<T>` filters into a parameterized `CouchbaseFilterExpression`.

See the [main README](../../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.NoSql.Couchbase
```

## Quick Example

```csharp
using Vali_Flow.NoSql.Couchbase.Extensions;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

CouchbaseFilterExpression f = filter.ToCouchbase();

var query = $"SELECT * FROM `orders` WHERE {f.WhereClause}";
var options = new QueryOptions();
foreach (var (name, value) in f.Parameters)
    options.Parameter(name, value);

var result = await cluster.QueryAsync<Order>(query, options);
```

## Features

- Translates `ValiFlow<T>` expressions to a N1QL WHERE clause fragment
- Named parameters (`$p0`, `$p1`, …) instead of inline literals
- Backtick-quoted identifiers (`` `field` ``), N1QL/MySQL convention
- `LIKE`-based `Contains` / `StartsWith` / `EndsWith` — all three supported
- No Couchbase SDK dependency — pure text + parameter builder

## Limitations

- No Couchbase SDK dependency: this package only produces `WhereClause` + `Parameters`. Executing the query (via `ICluster.QueryAsync`, `IBucket`, etc.) is entirely the consumer's responsibility.
- Empty `In` list throws `InvalidOperationException` — filter the empty case before building the query.
- `decimal` values are bound as their invariant-culture string representation, not a native numeric parameter.

## License

MIT
