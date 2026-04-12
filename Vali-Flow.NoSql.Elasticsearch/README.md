# Vali-Flow.NoSql.Elasticsearch

Elasticsearch Query DSL builder that translates `ValiFlow<T>` filters into native `Query` objects.

See the [main README](../../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.NoSql.Elasticsearch
```

## Quick Example

```csharp
using Vali_Flow.NoSql.Elasticsearch.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.Category, "Electronics")
    .GreaterThanOrEqualTo(x => x.Price, 100m)
    .Contains(x => x.Name, "phone");

Query esQuery = filter.ToElasticsearch();

var response = await client.SearchAsync<Product>(s => s.Query(esQuery));
```

## Features

- Translates `ValiFlow<T>` expressions to Elasticsearch Query DSL
- Native query types: `TermQuery`, `RangeQuery`, `WildcardQuery`, `BoolQuery`
- Pattern matching: wildcard queries for Contains, StartsWith, EndsWith
- Custom value converters for complex types
- Compatible with Elastic.Clients.Elasticsearch

## License

MIT
