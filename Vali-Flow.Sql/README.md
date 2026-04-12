# Vali-Flow.Sql

SQL query builder that translates `ValiFlow<T>` filters into parameterized SQL for Dapper / ADO.NET.

Supports SQL Server, PostgreSQL, MySQL, and SQLite dialects.

See the [main README](../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.Sql
```

## Quick Example

```csharp
using Vali_Flow.Sql.Extensions;
using Vali_Flow.Sql.Dialects;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

var result = filter.ToSql(new PostgreSqlDialect());

var orders = await connection.QueryAsync<Order>(
    $"SELECT * FROM orders WHERE {result.Sql}",
    result.Parameters);
```

## Dialects

- `SqlServerDialect` — SQL Server (T-SQL)
- `PostgreSqlDialect` — PostgreSQL
- `MySqlDialect` — MySQL
- `SqliteDialect` — SQLite

## Features

- Parameterized queries (SQL injection safe)
- `SqlQueryBuilder<T>` for full SELECT statements with JOIN, GROUP BY, aggregates
- Dialect-specific parameter prefixes and quoting
- Compatible with Dapper, ADO.NET, and raw SQL executors

## License

MIT
