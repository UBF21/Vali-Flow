# Vali-Flow

## Overview

Vali-Flow is a comprehensive .NET library ecosystem for building reusable, composable query criteria with a fluent API. It enables you to define filter logic once using a simple, expression-based DSL and translate it into:

- **Entity Framework Core** queries (async)
- **Parameterized SQL** for Dapper / ADO.NET (SQL Server, PostgreSQL, MySQL, SQLite)
- **MongoDB BSON** filters
- **Elasticsearch Query DSL**
- **Redis (RediSearch)** queries
- **AWS DynamoDB** filter expressions
- **In-memory** evaluation (LINQ-to-Objects)

All built on **Vali-Flow.Core** — a lightweight expression builder with zero additional dependencies.

**Supported platforms:** .NET 8.0, .NET 9.0

---

## The Problem It Solves

When working with multiple data stores or ORM patterns, you typically scatter filter logic across repositories, duplicate predicates per store, or couple business logic to data access code:

```csharp
// ❌ Traditional approach: filter logic is scattered
public async Task<List<Order>> GetActiveOrdersEF(DbContext db, decimal minTotal)
{
    return await db.Orders
        .Where(o => o.Status == "Active" && o.Total > minTotal)
        .ToListAsync();
}

public List<Order> GetActiveOrdersMongo(IMongoCollection<Order> coll, decimal minTotal)
{
    return coll.Find(Builders<Order>.Filter.And(
        Builders<Order>.Filter.Eq(o => o.Status, "Active"),
        Builders<Order>.Filter.Gt(o => o.Total, minTotal)
    )).ToList();
}

public DataTable GetActiveOrdersSQL(SqlConnection conn, decimal minTotal)
{
    var cmd = new SqlCommand(
        "SELECT * FROM Orders WHERE Status = @status AND Total > @total", conn);
    cmd.Parameters.AddWithValue("@status", "Active");
    cmd.Parameters.AddWithValue("@total", minTotal);
    // ...
}
```

**With Vali-Flow:**

```csharp
// ✅ Single filter definition
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, minTotal);

// Use the same filter everywhere
var efOrders = await new ValiFlowEvaluator<Order>(dbContext)
    .EvaluateQueryAsync(new BasicSpecification<Order>().WithFilter(filter));

var mongoOrders = mongoCollection.Find(filter.ToMongo()).ToList();

var sqlResult = filter.ToSql(new SqlServerDialect());
var sqlOrders = await connection.QueryAsync<Order>(
    $"SELECT * FROM Orders WHERE {sqlResult.Sql}",
    sqlResult.Parameters);
```

---

## Package Ecosystem

### Core Package

| Package | Purpose | Version |
|---------|---------|---------|
| **Vali-Flow.Core** | Fluent expression builder (`ValiFlow<T>`) - shared by all packages | [2.0.0](https://www.nuget.org/packages/Vali-Flow.Core) |

### Data Access Packages

| Package | Purpose | Target | Version |
|---------|---------|--------|---------|
| **Vali-Flow** | EF Core async evaluator + specifications (read/write) | `DbContext` | [1.1.0](https://www.nuget.org/packages/Vali-Flow) |
| **Vali-Flow.InMemory** | Synchronous in-memory evaluator for testing & caching | `IEnumerable<T>` | [1.0.0](https://www.nuget.org/packages/Vali-Flow.InMemory) |
| **Vali-Flow.Sql** | SQL query builder for parameterized queries | Dapper / ADO.NET | [1.0.0](https://www.nuget.org/packages/Vali-Flow.Sql) |

### NoSQL Packages

| Package | Database | Output Type | Version |
|---------|----------|-------------|---------|
| **Vali-Flow.NoSql.MongoDB** | MongoDB | `BsonDocument` | [1.0.0](https://www.nuget.org/packages/Vali-Flow.NoSql.MongoDB) |
| **Vali-Flow.NoSql.Elasticsearch** | Elasticsearch | `Query` (Elastic.Clients) | [1.0.0](https://www.nuget.org/packages/Vali-Flow.NoSql.Elasticsearch) |
| **Vali-Flow.NoSql.Redis** | Redis (RediSearch) | Query string | [1.0.0](https://www.nuget.org/packages/Vali-Flow.NoSql.Redis) |
| **Vali-Flow.NoSql.DynamoDB** | AWS DynamoDB | `DynamoFilterExpression` | [1.0.0](https://www.nuget.org/packages/Vali-Flow.NoSql.DynamoDB) |

### Architecture

```
Vali-Flow.Core  (expression builder — ValiFlow<T>)
       │
       ├─── Vali-Flow                    (EF Core async)
       ├─── Vali-Flow.InMemory           (sync in-memory)
       ├─── Vali-Flow.Sql                (SQL: SQL Server, PostgreSQL, MySQL, SQLite)
       │
       └─── Vali-Flow.NoSql
               ├─── Vali-Flow.NoSql.MongoDB        (MongoDB BSON)
               ├─── Vali-Flow.NoSql.Elasticsearch  (Elasticsearch Query DSL)
               ├─── Vali-Flow.NoSql.Redis          (RediSearch)
               └─── Vali-Flow.NoSql.DynamoDB       (DynamoDB filter expressions)
```

---

## Quick Start

### 1. Define a filter once

```csharp
using Vali_Flow.Core;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m)
    .IsAfter(x => x.CreatedAt, DateTime.UtcNow.AddDays(-30));
```

### 2. Use it with EF Core

```csharp
using Vali_Flow;

var evaluator = new ValiFlowEvaluator<Order>(dbContext);
var spec = new BasicSpecification<Order>()
    .WithFilter(filter)
    .WithAsNoTracking(true)
    .AddInclude(x => x.Customer);

var orders = await evaluator.EvaluateQueryAsync(spec, cancellationToken);
```

### 3. Or with SQL/Dapper

```csharp
using Vali_Flow.Sql.Extensions;
using Vali_Flow.Sql.Dialects;

var result = filter.ToSql(new PostgreSqlDialect());

var orders = await connection.QueryAsync<Order>(
    $"SELECT * FROM orders WHERE {result.Sql}",
    result.Parameters);
```

### 4. Or with MongoDB

```csharp
using Vali_Flow.NoSql.MongoDB.Extensions;

var bsonFilter = filter.ToMongo();
var orders = await collection.Find(bsonFilter).ToListAsync();
```

### 5. Or in-memory (testing)

```csharp
using Vali_Flow.InMemory;

var evaluator = new ValiFlowEvaluator<Order, int>(orders, null, x => x.Id);
var filtered = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    valiFlow: filter);
```

---

## Installation

**Install the package(s) you need:**

```bash
# EF Core (production)
dotnet add package Vali-Flow

# In-memory (testing / caching)
dotnet add package Vali-Flow.InMemory

# SQL queries (Dapper / ADO.NET)
dotnet add package Vali-Flow.Sql

# MongoDB
dotnet add package Vali-Flow.NoSql.MongoDB

# Elasticsearch
dotnet add package Vali-Flow.NoSql.Elasticsearch

# Redis (RediSearch)
dotnet add package Vali-Flow.NoSql.Redis

# AWS DynamoDB
dotnet add package Vali-Flow.NoSql.DynamoDB
```

All packages automatically include **Vali-Flow.Core** as a transitive dependency.

---

## Core Features

### Fluent Filter DSL (`ValiFlow<T>`)

Build complex filters with a natural, chainable API:

```csharp
var filter = new ValiFlow<Product>()
    // Comparison
    .EqualTo(x => x.Category, "Electronics")
    .GreaterThanOrEqualTo(x => x.Price, 100m)
    // String operations
    .Contains(x => x.Name, "phone")
    .StartsWith(x => x.Sku, "PROD")
    // Numeric ranges
    .Between(x => x.Quantity, 1, 1000)
    // Dates
    .IsAfter(x => x.CreatedAt, DateTime.UtcNow.AddDays(-90))
    // Collection
    .NotEmpty(x => x.Reviews)
    // Boolean
    .IsTrue(x => x.IsActive)
    // Logical operators
    .Or()
    .EqualTo(x => x.Category, "Accessories");
```

See [Vali-Flow.Core](https://github.com/UBF21/vali-flow-core) for the full list of 50+ predicates.

### Specifications

Encapsulate query criteria, ordering, pagination, and eager loading:

```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(filter)
    .WithOrderBy(x => x.CreatedAt, ascending: false)
    .AddThenBy(x => x.Total, ascending: true)
    .WithPagination(page: 1, pageSize: 20)
    .AddInclude(x => x.Customer)
    .AddInclude(x => x.OrderLines)
    .WithAsNoTracking(true);
```

### EF Core: Read Operations

```csharp
var evaluator = new ValiFlowEvaluator<Order>(dbContext);

// Existence and count
bool exists = await evaluator.EvaluateAnyAsync(spec);
int count   = await evaluator.EvaluateCountAsync(spec);

// Single entities
Order? first = await evaluator.EvaluateGetFirstAsync(spec);
Order? last  = await evaluator.EvaluateGetLastAsync(spec);

// Full query
IQueryable<Order> query = await evaluator.EvaluateQueryAsync(spec);

// Distinct and duplicates
IQueryable<Order> distinct   = await evaluator.EvaluateDistinctAsync(spec, x => x.CustomerId);
IQueryable<Order> duplicates = await evaluator.EvaluateDuplicatesAsync(spec, x => x.CustomerId);

// Aggregates
decimal minTotal = await evaluator.EvaluateMinAsync(spec, x => x.Total);
decimal maxTotal = await evaluator.EvaluateMaxAsync(spec, x => x.Total);
decimal avgTotal = await evaluator.EvaluateAverageAsync(spec, x => x.Total);
decimal sumTotal = await evaluator.EvaluateSumAsync(spec, x => x.Total);

// Grouped aggregates
Dictionary<string, int> countByStatus = 
    await evaluator.EvaluateCountByGroupAsync(spec, x => x.Status);

Dictionary<string, decimal> sumByStatus = 
    await evaluator.EvaluateSumByGroupAsync(spec, x => x.Status, x => x.Total);
```

### EF Core: Write Operations

```csharp
var evaluator = new ValiFlowEvaluator<Order>(dbContext);

// Single entity
var added   = await evaluator.AddAsync(order, saveChanges: true);
var updated = await evaluator.UpdateAsync(order, saveChanges: true);
await evaluator.DeleteAsync(order, saveChanges: true);

// Batch
await evaluator.AddRangeAsync(orders);
await evaluator.UpdateRangeAsync(orders);
await evaluator.DeleteRangeAsync(orders);

// Conditional delete
await evaluator.DeleteByConditionAsync(
    condition: x => x.Status == "Expired" && x.CreatedAt < cutoffDate);

// Upsert (insert if not found, update otherwise)
var upserted = await evaluator.UpsertAsync(
    entity: order,
    matchCondition: x => x.Id == order.Id);

// Bulk operations (via EFCore.BulkExtensions)
await evaluator.BulkInsertAsync(orders, new BulkConfig { BatchSize = 5000 });
await evaluator.BulkUpdateAsync(orders, new BulkConfig { BatchSize = 5000 });
await evaluator.BulkInsertOrUpdateAsync(orders);

// Transactions
await evaluator.ExecuteTransactionAsync(async () =>
{
    await evaluator.AddAsync(order1, saveChanges: false);
    await evaluator.UpdateAsync(order2, saveChanges: false);
    await evaluator.SaveChangesAsync();
});
```

### SQL Query Builder (Dapper / ADO.NET)

Four dialects out of the box: SQL Server, PostgreSQL, MySQL, SQLite.

```csharp
// Simple WHERE clause
var result = filter.ToSql(new PostgreSqlDialect());
var orders = await connection.QueryAsync<Order>(
    $"SELECT * FROM orders WHERE {result.Sql}",
    result.Parameters);

// Full SELECT with JOIN, GROUP BY, aggregates
var query = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .Select(x => x.Id, x => x.Status, x => x.Total)
    .From("orders")
    .Where(w => w.EqualTo(x => x.Status, "Active"))
    .OrderBy(x => x.CreatedAt, ascending: false)
    .Paginate(page: 1, pageSize: 20);

var result = query.Build();
```

### In-Memory Evaluator (Testing / Caching)

Synchronous, dependency-free evaluation against `IEnumerable<T>`:

```csharp
var evaluator = new ValiFlowEvaluator<Order, int>(orders, null, x => x.Id);

var filter = new ValiFlow<Order>().EqualTo(x => x.Status, "Active");

int count  = evaluator.EvaluateCount(orders, filter);
Order? first = evaluator.GetFirst(orders, filter);

IEnumerable<Order> filtered = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    valiFlow: filter);

Dictionary<string, int> countByStatus = 
    evaluator.EvaluateCountByGroup(orders, x => x.Status, filter);
```

---

## NoSQL Support

### MongoDB

```csharp
using Vali_Flow.NoSql.MongoDB.Extensions;

var filter = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Age, 18);

BsonDocument bsonFilter = filter.ToMongo();
var users = await collection.Find(bsonFilter).ToListAsync();
```

### Elasticsearch

```csharp
using Vali_Flow.NoSql.Elasticsearch.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.Category, "Electronics")
    .GreaterThanOrEqualTo(x => x.Price, 100m)
    .Contains(x => x.Name, "phone");

Query esQuery = filter.ToElasticsearch();
var response = await client.SearchAsync<Product>(s => s.Query(esQuery));
```

### Redis (RediSearch)

```csharp
using Vali_Flow.NoSql.Redis.Extensions;

string redisQuery = filter.ToRedisSearch();
var results = db.FT().Search("idx:products", new Query(redisQuery));
```

### DynamoDB

```csharp
using Vali_Flow.NoSql.DynamoDB.Extensions;

DynamoFilterExpression f = filter.ToDynamoDB();

var request = new ScanRequest
{
    TableName                 = "Orders",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};
```

---

## Documentation

- **[Full Feature Guide](docs/FEATURES.md)** — Detailed examples for each package
- **[Architecture Guide](docs/ARCHITECTURE.md)** — Design patterns and decision rationale
- **[SQL Dialects Reference](Vali-Flow.Sql/README.md)** — SQL Builder capabilities
- **[Vali-Flow.Core](https://github.com/UBF21/vali-flow-core)** — Expression builder predicates

---

## License

Licensed under the [MIT License](LICENSE).  
Copyright © 2025 Felipe Rafael Montenegro Morriberon. All rights reserved.

---

## Support

- **Issues & Feature Requests:** [GitHub Issues](https://github.com/UBF21/vali-flow/issues)
- **Discussions:** [GitHub Discussions](https://github.com/UBF21/vali-flow/discussions)

### Contribute

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

If this project helps you, consider supporting its development:
- **Latin America** — [MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **International** — [PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)
