# Vali-Flow.Sql

**SQL query builder that translates `ValiFlow<T>` filters into parameterized SQL.**

Write filters once using Vali-Flow's fluent API, then execute them against any SQL database using Dapper, ADO.NET, or any query executor — no ORM required.

Part of the [Vali-Flow ecosystem](https://github.com/UBF21/vali-flow) — write filters once, use them everywhere (EF Core, SQL, in-memory, MongoDB, Elasticsearch, etc).

**Supported platforms:** .NET 8.0, .NET 9.0

---

## When to Use

✅ **Use Vali-Flow.Sql for:**
- Working with raw SQL or Dapper (no EF Core)
- High-performance queries over EF Core (avoiding ORM overhead)
- Complex queries with CTEs, window functions, or dialect-specific features
- Microservices using lightweight data access
- Legacy systems where ORM isn't suitable
- Multi-tenant applications with dynamic table/schema routing

❌ **Don't use if:**
- You're already using Vali-Flow + EF Core (use that instead)
- You need full object mapping and change tracking (use an ORM)

---

## Installation

```bash
dotnet add package Vali-Flow.Sql
```

**Dependencies:**
- Vali-Flow.Core (v2.0.0+) — expression builder
- No database dependencies — bring your own `SqlConnection` or Dapper

---

## Quick Start

### 1. Create a filter

```csharp
using Vali_Flow.Core;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m)
    .IsAfter(x => x.CreatedAt, DateTime.UtcNow.AddDays(-30));
```

### 2. Translate to SQL

```csharp
using Vali_Flow.Sql.Extensions;
using Vali_Flow.Sql.Dialects;

// Choose your dialect
var dialect = new PostgreSqlDialect();  // or SqlServerDialect, MySqlDialect, SqliteDialect
var result = filter.ToSql(dialect);

// result.Sql = "Status = @p0 AND Total > @p1 AND CreatedAt > @p2"
// result.Parameters = { "p0" => "Active", "p1" => 100m, "p2" => <date> }
```

### 3. Execute with Dapper or ADO.NET

```csharp
using Dapper;

var orders = await connection.QueryAsync<Order>(
    $"SELECT * FROM orders WHERE {result.Sql}",
    result.Parameters);
```

---

## Dialects

| Dialect | Database | Parameter Prefix | Column Quoting |
|---------|----------|------------------|----------------|
| `SqlServerDialect` | SQL Server | `@` | `[column]` |
| `PostgreSqlDialect` | PostgreSQL | `@` | `"column"` |
| `MySqlDialect` | MySQL | `@` | `` `column` `` |
| `SqliteDialect` | SQLite | `@` | `"column"` |

---

## Filter → WHERE Clause

### Basic Comparisons

```csharp
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")           // Status = @p0
    .NotEqualTo(x => x.Status, "Deleted")       // Status <> @p1
    .GreaterThan(x => x.Total, 100m)            // Total > @p2
    .GreaterThanOrEqualTo(x => x.Total, 100m)   // Total >= @p3
    .LessThan(x => x.Total, 1000m)              // Total < @p4
    .LessThanOrEqualTo(x => x.Total, 1000m);    // Total <= @p5

var sql = filter.ToSql(new SqlServerDialect());
// WHERE Status = @p0 AND Status <> @p1 AND Total > @p2 AND Total >= @p3 AND Total < @p4 AND Total <= @p5
```

### String Operations

```csharp
var filter = new ValiFlow<Order>()
    .Contains(x => x.Description, "urgent")     // Description LIKE %urgent%
    .StartsWith(x => x.Code, "ORD")             // Code LIKE ORD%
    .EndsWith(x => x.Code, "001")               // Code LIKE %001
    .IsNullOrEmpty(x => x.Notes);               // Notes IS NULL OR Notes = ''

var sql = filter.ToSql(new SqlServerDialect());
```

### Date Operations

```csharp
var filter = new ValiFlow<Order>()
    .IsAfter(x => x.CreatedAt, startDate)       // CreatedAt > @p0
    .IsAfterOrEqualTo(x => x.CreatedAt, startDate)
    .IsBefore(x => x.CreatedAt, endDate)        // CreatedAt < @p1
    .IsBeforeOrEqualTo(x => x.CreatedAt, endDate)
    .IsBetween(x => x.CreatedAt, startDate, endDate); // CreatedAt BETWEEN @p2 AND @p3

var sql = filter.ToSql(new PostgreSqlDialect());
```

### Numeric Operations

```csharp
var filter = new ValiFlow<Order>()
    .IsInRange(x => x.Total, min: 100m, max: 1000m)  // Total BETWEEN @p0 AND @p1
    .IsPositive(x => x.Total)                        // Total > 0
    .IsNegative(x => x.Discount)                     // Discount < 0
    .IsZero(x => x.Balance);                         // Balance = 0

var sql = filter.ToSql(new SqlServerDialect());
```

### Collections

```csharp
var filter = new ValiFlow<Order>()
    .In(x => x.Status, "Active", "Pending", "Processing")  // Status IN (@p0, @p1, @p2)
    .NotIn(x => x.Status, "Deleted", "Archived");          // Status NOT IN (@p3, @p4)

var sql = filter.ToSql(new MySqlDialect());
```

### Complex Logic

```csharp
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .And()
    .GreaterThan(x => x.Total, 100m);
    // (Status = @p0) AND (Total > @p1)

var filter2 = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .Or()
    .EqualTo(x => x.Status, "Pending");
    // (Status = @p0) OR (Status = @p1)

var sql = filter.ToSql(new SqliteDialect());
```

---

## Building Full Queries with SqlQueryBuilder

Beyond WHERE clauses, use `SqlQueryBuilder<T>` for complete SELECT statements:

```csharp
using Vali_Flow.Sql.Builders;

var builder = new SqlQueryBuilder<Order>(new PostgreSqlDialect())
    .Select("id", "status", "total", "created_at")
    .From("orders")
    .Where(new ValiFlow<Order>()
        .EqualTo(x => x.Status, "Active")
        .GreaterThan(x => x.Total, 100m))
    .OrderBy("created_at", ascending: false)
    .Limit(50)
    .Offset(0);

var result = builder.Build();
// SELECT id, status, total, created_at 
// FROM orders 
// WHERE status = @p0 AND total > @p1 
// ORDER BY created_at DESC 
// LIMIT 50 OFFSET 0

var orders = await connection.QueryAsync<Order>(result.Sql, result.Parameters);
```

### With JOINs

```csharp
var builder = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .Select("o.id", "o.status", "o.total", "c.name as customer_name")
    .From("orders o")
    .InnerJoin("customers c", "o.customer_id = c.id")
    .Where(new ValiFlow<Order>()
        .EqualTo(x => x.Status, "Active"))
    .OrderBy("o.created_at");

var result = builder.Build();
var orders = await connection.QueryAsync<dynamic>(result.Sql, result.Parameters);
```

### With GROUP BY & Aggregates

```csharp
var builder = new SqlQueryBuilder<Order>(new PostgreSqlDialect())
    .Select("status", "COUNT(*) as count", "SUM(total) as total_revenue")
    .From("orders")
    .Where(new ValiFlow<Order>()
        .IsAfter(x => x.CreatedAt, DateTime.Now.AddDays(-30)))
    .GroupBy("status")
    .Having("COUNT(*) > 5")
    .OrderBy("total_revenue", ascending: false);

var result = builder.Build();
var stats = await connection.QueryAsync<dynamic>(result.Sql, result.Parameters);
```

### With Subqueries & CTEs

```csharp
var builder = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .WithCommonTableExpression("recent_orders", 
        "SELECT * FROM orders WHERE created_at > DATEADD(day, -30, GETDATE())")
    .Select("status", "COUNT(*) as count")
    .From("recent_orders")
    .GroupBy("status");

var result = builder.Build();
var stats = await connection.QueryAsync<dynamic>(result.Sql, result.Parameters);
```

---

## Working with Dapper

### Basic Query

```csharp
public async Task<List<Order>> GetActiveOrdersAsync(SqlConnection conn)
{
    var filter = new ValiFlow<Order>()
        .EqualTo(x => x.Status, "Active")
        .GreaterThan(x => x.Total, 100m);

    var sql = filter.ToSql(new SqlServerDialect());

    return (await conn.QueryAsync<Order>(
        $"SELECT id, status, total, created_at FROM orders WHERE {sql.Sql}",
        sql.Parameters)).ToList();
}
```

### With Pagination

```csharp
public async Task<(List<Order> Items, int Total)> GetOrdersPagedAsync(
    SqlConnection conn, 
    ValiFlow<Order> filter,
    int page,
    int pageSize)
{
    var filterSql = filter.ToSql(new PostgreSqlDialect());
    var offset = (page - 1) * pageSize;

    var sql = $@"
        SELECT id, status, total, created_at 
        FROM orders 
        WHERE {filterSql.Sql}
        ORDER BY created_at DESC
        LIMIT @limit OFFSET @offset";

    var parameters = new DynamicParameters(filterSql.Parameters);
    parameters.Add("limit", pageSize);
    parameters.Add("offset", offset);

    var items = (await conn.QueryAsync<Order>(sql, parameters)).ToList();

    var countSql = $"SELECT COUNT(*) FROM orders WHERE {filterSql.Sql}";
    var total = await conn.QueryFirstAsync<int>(countSql, filterSql.Parameters);

    return (items, total);
}
```

### Bulk Operations

```csharp
public async Task InsertOrdersAsync(SqlConnection conn, List<Order> orders)
{
    var insertSql = @"
        INSERT INTO orders (id, status, total, created_at) 
        VALUES (@id, @status, @total, @created_at)";

    using (var transaction = conn.BeginTransaction())
    {
        foreach (var order in orders)
        {
            await conn.ExecuteAsync(insertSql, order, transaction);
        }
        transaction.Commit();
    }
}

// Or use Dapper multi-execute for better perf
public async Task InsertOrdersBatchAsync(SqlConnection conn, List<Order> orders)
{
    var insertSql = @"
        INSERT INTO orders (id, status, total, created_at) 
        VALUES (@id, @status, @total, @created_at)";

    await conn.ExecuteAsync(insertSql, orders);
}
```

---

## ADO.NET Integration

Apply parameters directly to `SqlCommand`:

```csharp
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

var sql = filter.ToSql(new SqlServerDialect());

using (var conn = new SqlConnection(connectionString))
using (var cmd = new SqlCommand($"SELECT * FROM orders WHERE {sql.Sql}", conn))
{
    sql.ApplyTo(cmd); // Applies all parameters

    await conn.OpenAsync();
    using (var reader = await cmd.ExecuteReaderAsync())
    {
        var orders = new List<Order>();
        while (await reader.ReadAsync())
        {
            orders.Add(new Order
            {
                Id = (int)reader["id"],
                Status = (string)reader["status"],
                Total = (decimal)reader["total"],
            });
        }
        return orders;
    }
}
```

---

## Performance Tips

1. **Parameterized queries** — always use (no string interpolation of values)
2. **Index your WHERE columns** — same as any SQL optimization
3. **Use proper dialect** — wrong quoting or parameter syntax = runtime errors
4. **Batch updates/inserts** — `conn.ExecuteAsync(sql, enumerable)` is faster than looping
5. **Combine with query-specific needs** — `SqlQueryBuilder` if you need full control (JOINs, CTEs, etc)

---

## Comparing to EF Core

| Feature | Vali-Flow (EF Core) | Vali-Flow.Sql |
|---------|-------------------|--------------|
| **Change tracking** | Yes | No |
| **Lazy loading** | Yes | No |
| **Includes** | Yes | Manual JOINs |
| **Migrations** | Yes | Manual DDL |
| **Performance** | ORM overhead | Raw SQL speed |
| **Best for** | Complex apps | High-perf, simple queries |
| **Learning curve** | Moderate | Low |

---

## Same Filter, Multiple Targets

```csharp
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

// EF Core
var efEvaluator = new ValiFlowEvaluator<Order>(dbContext);
var dbOrders = await efEvaluator.EvaluateQueryAsync(
    new QuerySpecification<Order>().WithFilter(filter));

// SQL + Dapper
var sqlResult = filter.ToSql(new PostgreSqlDialect());
var sqlOrders = await connection.QueryAsync<Order>(
    $"SELECT * FROM orders WHERE {sqlResult.Sql}",
    sqlResult.Parameters);

// In-memory
var memEvaluator = new ValiFlowEvaluator<Order, int>(cachedOrders, filter, x => x.Id);
var cachedResults = memEvaluator.EvaluateAll();

// Same logic, three backends ✅
```

---

## Full Ecosystem

- **Vali-Flow.Core** — expression builder (`ValiFlow<T>`)
- **Vali-Flow** — EF Core async evaluator
- **Vali-Flow.InMemory** — synchronous in-memory evaluator
- **Vali-Flow.Sql** — parameterized SQL translator (this package)
- **Vali-Flow.NoSql.\*** — MongoDB, Elasticsearch, Redis, DynamoDB adapters

See [main repository](https://github.com/UBF21/vali-flow) for complete docs.

---

## License

MIT
