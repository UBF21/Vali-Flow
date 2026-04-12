# Vali-Flow.Sql

Translates a `ValiFlow<T>` fluent filter into a parameterized SQL `WHERE` clause, and provides `SqlQueryBuilder<T>` for full `SELECT` statements with JOINs, GROUP BY, aggregates, and pagination — ready for Dapper, ADO.NET, or any raw SQL executor.

## Install

```bash
dotnet add package Vali-Flow.Sql
```

## Quick Start — WHERE clause only

```csharp
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Extensions;
using Vali_Flow.Sql.Dialects;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

SqlResult result = filter.ToSql(new SqlServerDialect());
// result.Sql        → "([Status] = @p0 AND [Total] > @p1)"
// result.Parameters → { "@p0": "Active", "@p1": 100m }
```

### With Dapper

```csharp
var result = filter.ToSql(new PostgreSqlDialect());

var orders = await connection.QueryAsync<Order>(
    $"SELECT * FROM orders WHERE {result.Sql}",
    result.Parameters);
```

### With ADO.NET

```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = $"SELECT * FROM orders WHERE {result.Sql}";
result.ApplyTo(cmd); // applies all parameters directly to the IDbCommand
```

## Dialects

| Class | Column quoting | Case-insensitive strings |
|-------|---------------|--------------------------|
| `SqlServerDialect` | `[col]` | `LOWER()` |
| `PostgreSqlDialect` | `"col"` | Native `ILIKE` |
| `MySqlDialect` | `` `col` `` | Case-insensitive `LIKE` |
| `SqliteDialect` | `"col"` | `LOWER()` |

All dialects use `@p0`, `@p1`, … for parameter names.

## SqlQueryBuilder\<T\> — full SELECT statements

### Basic SELECT with filter, order, and pagination

```csharp
SqlResult result = new SqlQueryBuilder<Order>(new PostgreSqlDialect())
    .Select(x => x.Id, x => x.Status, x => x.Total)
    .From("orders")
    .Where(w => w.EqualTo(x => x.Status, "Active"))
    .OrderBy(x => x.CreatedAt, ascending: false)
    .Paginate(page: 1, pageSize: 20)
    .Build();
```

### Aggregates and GROUP BY

```csharp
SqlResult result = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .From("orders")
    .GroupBy(x => x.Status)
    .Having(h => h.CountGreaterThan(5))
    .SelectAggregates(b => b
        .Column(x => x.Status)
        .Sum(x => x.Total, alias: "TotalRevenue")
        .Count(alias: "OrderCount"))
    .Build();
```

### JOINs

```csharp
SqlResult result = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .Select(x => x.Id, x => x.Total)
    .From("orders", alias: "o")
    .InnerJoin("customers", alias: "c", on: "o.CustomerId = c.Id")
    .LeftJoin("shipping_addresses", alias: "sa", on: "o.ShippingAddressId = sa.Id")
    .Where(w => w.EqualTo(x => x.Status, "Active"))
    .Build();
```

### UNION

```csharp
SqlResult result = new SqlQueryBuilder<Order>(new PostgreSqlDialect())
    .From("orders_2024")
    .Where(w => w.EqualTo(x => x.Status, "Completed"))
    .Union(
        new SqlQueryBuilder<Order>(new PostgreSqlDialect())
            .From("orders_2025")
            .Where(w => w.EqualTo(x => x.Status, "Completed")))
    .Build();
```

### EXISTS subquery

```csharp
SqlResult result = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .Select(x => x.Id, x => x.Status)
    .From("orders", alias: "o")
    .WhereExists(
        new SqlQueryBuilder<OrderLine>(new SqlServerDialect())
            .From("order_lines", alias: "ol")
            .Where(w => w.EqualTo(x => x.ProductId, 42))
            .WithRawCondition("ol.OrderId = o.Id"))
    .Build();
```

### CASE WHEN

```csharp
SqlResult result = new SqlQueryBuilder<Order>(new PostgreSqlDialect())
    .Select(x => x.Id)
    .From("orders")
    .SelectCase(b => b
        .When("Status = 'Active'", then: "'open'")
        .When("Status = 'Cancelled'", then: "'closed'")
        .Else("'unknown'"),
        alias: "StatusLabel")
    .Build();
```

### Query hints

```csharp
// SQL Server: WITH (NOLOCK)
SqlResult result = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .From("orders")
    .WithHint("NOLOCK")
    .Build();
```

## What it supports

| Feature | `ToSql()` | `SqlQueryBuilder<T>` |
|---------|:---------:|:--------------------:|
| WHERE clause | Yes | Yes |
| SELECT columns | — | Yes |
| JOIN (INNER / LEFT / RIGHT / FULL) | — | Yes |
| GROUP BY + HAVING | — | Yes |
| Aggregates (COUNT / SUM / AVG / MIN / MAX) | — | Yes |
| ORDER BY + pagination (OFFSET/FETCH / LIMIT) | — | Yes |
| Top-N | — | Yes |
| UNION / UNION ALL | — | Yes |
| EXISTS subquery | — | Yes |
| CASE WHEN | — | Yes |
| Table schema (`schema.table`) | — | Yes |
| Query hints (NOLOCK, etc.) | — | Yes |
| Column aliases | — | Yes |
| Expression overload | Yes | — |

## Expression overload

When you already have a compiled predicate:

```csharp
Expression<Func<Order, bool>> expr = o => o.Status == "Active" && o.Total > 100m;
SqlResult result = expr.ToSql(new MySqlDialect());
```

## Full documentation

[vali-flow-docs.netlify.app/docs/adapters/sql/overview](https://vali-flow-docs.netlify.app/docs/adapters/sql/overview)

## Contributing

Contributions, issues, and feature requests are welcome. Feel free to open a pull request or an issue on [GitHub](https://github.com/UBF21/vali-flow).

If this package is useful to you, consider supporting its development:

- **Latin America** — [MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **International** — [PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)

## License

Licensed under the [MIT License](LICENSE).  
Copyright &copy; 2025 Felipe Rafael Montenegro Morriberon. All rights reserved.
