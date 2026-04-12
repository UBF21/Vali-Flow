# Getting Started with Vali-Flow

This guide walks you through installing and using the Vali-Flow ecosystem from scratch. You will build your first filter, execute it, and understand which package to reach for in each scenario.

## Prerequisites

- .NET 8 or .NET 9
- A C# project (any type: web API, console, test project, etc.)

---

## What is Vali-Flow?

Vali-Flow is a set of NuGet packages that let you express data access conditions as strongly-typed C# expression trees and then translate them to the persistence technology of your choice:

| Package | When to use |
|---------|------------|
| `Vali-Flow` | You use Entity Framework Core (relational DB) |
| `Vali-Flow.InMemory` | You need a synchronous in-memory evaluator (unit tests, caching) |
| `Vali-Flow.Sql` | You use Dapper, ADO.NET, or any raw SQL approach |
| `Vali-Flow.NoSql.MongoDB` | Your store is MongoDB |
| `Vali-Flow.NoSql.DynamoDB` | Your store is AWS DynamoDB |
| `Vali-Flow.NoSql.Elasticsearch` | Your store is Elasticsearch |
| `Vali-Flow.NoSql.Redis` | Your store is Redis (with RediSearch) |

All packages share the same `ValiFlow<T>` expression builder from `Vali-Flow.Core` — the builder itself is a transitive dependency, you never install it directly.

---

## 1. EF Core — Quickstart

### Install

```bash
dotnet add package Vali-Flow
```

### Define an entity

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public string Category { get; set; } = "";
}
```

### Build a filter

```csharp
using Vali_Flow.Core.Builder;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 10m)
    .LessThanOrEqualTo(x => x.Price, 500m);
```

`ValiFlow<T>` is a fluent builder — every method returns the same instance, so you chain conditions with `.`. All conditions are combined with logical AND by default.

### Create an evaluator

```csharp
using Vali_Flow.Classes.Evaluators;

// Assuming you have an EF Core DbContext
var evaluator = new ValiFlowEvaluator<Product>(context);
```

### Query

```csharp
using Vali_Flow.Classes.Specifications;

var spec = new QuerySpecification<Product>(filter);

// Get all matching products
IEnumerable<Product> products = await evaluator.EvaluateQueryAsync(spec);

// Get count
int count = await evaluator.EvaluateCountAsync(spec);

// Get first or null
Product? first = await evaluator.EvaluateGetFirstAsync(spec);
```

### Add, update, delete

```csharp
var newProduct = new Product { Name = "Widget", Price = 29.99m, IsActive = true };

// Add
await evaluator.AddAsync(newProduct);

// Update (entity already tracked)
newProduct.Price = 34.99m;
await evaluator.UpdateAsync(newProduct);

// Delete by condition — removes all matching without loading them first
await evaluator.DeleteByConditionAsync(filter);
```

---

## 2. In-Memory — Quickstart

Use `Vali-Flow.InMemory` when you want to filter an in-memory list with the exact same API:

```bash
dotnet add package Vali-Flow.InMemory
```

```csharp
using Vali_Flow.InMemory.Classes.Evaluators;

var products = new List<Product>
{
    new() { Id = 1, Name = "Widget", Price = 29.99m, IsActive = true },
    new() { Id = 2, Name = "Gadget", Price = 149.99m, IsActive = true },
    new() { Id = 3, Name = "Doohickey", Price = 9.99m, IsActive = false },
};

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 20m);

var evaluator = new ValiFlowEvaluator<Product, int>(
    initialData: products,
    filter: filter,
    getId: p => p.Id
);

// Synchronous — no async/await needed
IEnumerable<Product> result = evaluator.EvaluateQuery();
int count = evaluator.EvaluateCount();
```

---

## 3. SQL — Quickstart

Use `Vali-Flow.Sql` to generate a parameterized WHERE clause for Dapper or ADO.NET:

```bash
dotnet add package Vali-Flow.Sql
```

```csharp
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 10m);

var sql = filter.ToSql(new SqlServerDialect());
// sql.Sql        → "[IsActive] = @p0 AND [Price] > @p1"
// sql.Parameters → { "p0": true, "p1": 10 }

// Use with Dapper
var products = await connection.QueryAsync<Product>(
    $"SELECT * FROM Products WHERE {sql.Sql}",
    sql.Parameters
);
```

Available dialects: `SqlServerDialect`, `PostgreSqlDialect`, `MySqlDialect`, `SqliteDialect`, `OracleDialect`.

---

## 4. NoSQL — Quickstart

Each NoSQL package follows the same pattern: call `.ToXxx()` on the builder and pass the result to the driver.

**MongoDB:**
```csharp
BsonDocument filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 10m)
    .ToMongo();

var products = await collection.Find(filter).ToListAsync();
```

**Elasticsearch:**
```csharp
Query filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 10m)
    .ToElasticsearch();

var response = await client.SearchAsync<Product>(s => s.Query(filter));
```

**DynamoDB:**
```csharp
DynamoFilterExpression f = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 10m)
    .ToDynamoDB();

var request = new ScanRequest
{
    TableName                 = "Products",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};
```

**Redis (RediSearch):**
```csharp
string query = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 10m)
    .ToRedisSearch();

SearchResult result = db.FT().Search("idx:products", new Query(query));
```

---

## Logical operators

By default, conditions are AND-ed. Use `.Or()` and `.Not()` for other compositions:

```csharp
var cheapOrPremium = new ValiFlow<Product>()
    .Or(
        new ValiFlow<Product>().LessThan(x => x.Price, 20m),
        new ValiFlow<Product>().GreaterThan(x => x.Price, 1000m)
    );

var notDiscontinued = new ValiFlow<Product>()
    .Not(new ValiFlow<Product>().EqualTo(x => x.Category, "Discontinued"));
```

---

## Next steps

- **EF Core evaluator reference** → [`vali-flow-ef-core.md`](../vali-flow-ef-core.md)
- **In-Memory reference** → [`vali-flow-inmemory.md`](../vali-flow-inmemory.md)
- **SQL builder reference** → [`vali-flow-sql.md`](../vali-flow-sql.md)
- **Testing with InMemory** → [`testing-with-inmemory.md`](testing-with-inmemory.md)
- **Combining packages** → [`combining-packages.md`](combining-packages.md)
- **Architecture overview** → [`../architecture/overview.md`](../architecture/overview.md)
