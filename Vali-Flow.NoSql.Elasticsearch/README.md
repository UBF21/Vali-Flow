# Vali-Flow.NoSql.Elasticsearch

Translates a `ValiFlow<T>` fluent filter into an Elasticsearch `Query` object — ready to pass to `Search`, `Count`, `DeleteByQuery`, and any other operation that accepts a query.

## Install

```bash
dotnet add package Vali-Flow.NoSql.Elasticsearch
```

## Quick Start

```csharp
using Elastic.Clients.Elasticsearch;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Elasticsearch.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .In(x => x.Category, new[] { "Electronics", "Computers" })
    .GreaterThanOrEqualTo(x => x.Price, 50m)
    .LessThan(x => x.Price, 1500m)
    .IsNotNull(x => x.Sku);

Query esFilter = filter.ToElasticsearch();

var response = await client.SearchAsync<Product>(s => s
    .Index("products")
    .Query(esFilter)
    .Size(25));
```

## What it supports

| Operation | Elasticsearch query |
|-----------|---------------------|
| `EqualTo` | `term` |
| `NotEqualTo` | `bool { must_not: [term] }` |
| `GreaterThan` / `GreaterThanOrEqualTo` | `range { gt/gte }` |
| `LessThan` / `LessThanOrEqualTo` | `range { lt/lte }` |
| `Contains` / `StartsWith` / `EndsWith` | `wildcard` (case_insensitive: true) |
| `In` | `terms` |
| `IsNull` | `bool { must_not: [exists] }` |
| `IsNotNull` | `exists` |
| `And` / `Or` / `Not` | `bool.must` / `bool.should` / `bool.must_not` |

All supported IR node types are translated. No operations throw `NotSupportedException`.

## Custom value converter

Pass a `customConverter` delegate to handle domain types that are not in the built-in switch:

```csharp
// Represent a Money value object as a FieldValue.Double
Query filter = new ValiFlow<Product>()
    .LessThan(x => x.Price, new Money(500m, "USD"))
    .ToElasticsearch(value =>
    {
        if (value is Money m) return FieldValue.Double((double)m.Amount);
        return null; // fall through for all other types
    });
```

## Expression overload

When you already have a compiled predicate:

```csharp
Expression<Func<Order, bool>> expr = o => o.Total > 1000m && o.Status == "Shipped";
Query filter = expr.ToElasticsearch();
```

## Notes

- Depends on `Elastic.Clients.Elasticsearch` for query types only — no connection or execution logic.
- Field names mirror .NET property names. Configure your index mappings or serializer settings to map to custom Elasticsearch field names.
- Range queries use `NumberRangeQuery` and convert values to `double`. For date range queries, use a `customConverter` to produce `DateRangeQuery`.
- An empty `In` list produces `bool { must_not: [match_all] }` — always false.

## Contributing

Contributions, issues, and feature requests are welcome. Feel free to open a pull request or an issue on [GitHub](https://github.com/UBF21/vali-flow).

If this package is useful to you, consider supporting its development:

- **Latin America** — [MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **International** — [PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)

## License

Licensed under the [MIT License](LICENSE).  
Copyright &copy; 2025 Felipe Rafael Montenegro Morriberon. All rights reserved.

## Full documentation

[vali-flow-docs.netlify.app/docs/adapters/nosql/elasticsearch/overview](https://vali-flow-docs.netlify.app/docs/adapters/nosql/elasticsearch/overview)
