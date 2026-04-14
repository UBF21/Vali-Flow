# Vali-Flow

**Entity Framework Core async evaluator with fluent specifications for building reusable query logic and write operations.**

Part of the [Vali-Flow ecosystem](https://github.com/UBF21/vali-flow) — write filters once, use them everywhere (EF Core, SQL, MongoDB, Elasticsearch, etc).

**Supported platforms:** .NET 8.0, .NET 9.0

---

## What It Solves

Vali-Flow decouples filter logic from data access code. Instead of scattering LINQ predicates across repositories, write a single fluent filter definition and translate it to any data store:

```csharp
// ✅ Single filter, used everywhere
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m)
    .IsAfter(x => x.CreatedAt, DateTime.UtcNow.AddDays(-30));

// EF Core
var efOrders = await new ValiFlowEvaluator<Order>(dbContext)
    .EvaluateQueryAsync(new QuerySpecification<Order>().WithFilter(filter));

// SQL (Dapper)
var sqlResult = filter.ToSql(new PostgreSqlDialect());

// In-memory (testing)
var memoryOrders = new ValiFlowEvaluator<Order, int>(orders, filter, x => x.Id).EvaluateAll();
```

---

## Installation

```bash
dotnet add package Vali-Flow
```

**Dependencies:**
- Vali-Flow.Core (v2.0.0+) — expression builder
- Vali-Flow.Abstractions (v1.0.0+) — shared interfaces
- Microsoft.EntityFrameworkCore (v9.0+)
- EFCore.BulkExtensions (v8.1+) — for bulk operations

---

## Quick Start

### 1. Create an evaluator

```csharp
using Vali_Flow;
using Microsoft.EntityFrameworkCore;

var evaluator = new ValiFlowEvaluator<Order>(dbContext);
```

### 2. Build a specification with filter

```csharp
using Vali_Flow.Core;

var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .EqualTo(x => x.Status, "Active")
        .GreaterThan(x => x.Total, 100m))
    .AddInclude(x => x.Customer)
    .WithAsNoTracking(true)
    .WithOrderBy(x => x.CreatedAt)
    .WithPagination(page: 1, pageSize: 20);
```

### 3. Execute the query

```csharp
// Single result
var order = await evaluator.EvaluateAsync(spec);

// Multiple results
var orders = await evaluator.EvaluateQueryAsync(spec);

// Count
var total = await evaluator.EvaluateCountAsync(spec);

// Any / All
var hasActive = await evaluator.EvaluateAnyAsync(spec);

// Paginated
var paged = await evaluator.EvaluatePagedAsync(spec);
```

---

## Core Features

### Read Operations

| Method | Purpose |
|--------|---------|
| `EvaluateAsync(spec)` | Single entity by ID or condition |
| `EvaluateQueryAsync(spec)` | List of entities |
| `EvaluateAnyAsync(spec)` | Boolean exists check |
| `EvaluateCountAsync(spec)` | Entity count |
| `EvaluatePagedAsync(spec)` | Paginated results with metadata |
| `EvaluateDistinctAsync(selector)` | Unique values by selector |
| `EvaluateDuplicatesAsync(selector)` | Duplicated entities by selector |

### Aggregates

```csharp
// Sum, Min, Max, Average
var totalRevenue = await evaluator.EvaluateSumAsync(
    spec, x => x.Total);

var avgPrice = await evaluator.EvaluateAverageAsync(
    spec, x => x.Price);

var maxDate = await evaluator.EvaluateMaxAsync(
    spec, x => x.CreatedAt);
```

### Grouped Operations

```csharp
// Group by status, count per group
var countByStatus = await evaluator.EvaluateCountByGroupAsync(
    spec, x => x.Status);

// Top N per group
var top3OrdersByCustomer = await evaluator.EvaluateTopByGroupAsync(
    spec, 
    groupKey: x => x.CustomerId,
    pageSize: 3,
    orderBy: x => x.Total);

// Aggregate by group
var sumByRegion = await evaluator.EvaluateSumByGroupAsync(
    spec, x => x.Region, x => x.Revenue);
```

### Write Operations

```csharp
// Create
var newOrder = new Order { ... };
await evaluator.AddAsync(newOrder);
await evaluator.SaveChangesAsync();

// Update single
var order = await evaluator.EvaluateAsync(spec);
order.Status = "Shipped";
await evaluator.UpdateAsync(order);

// Batch operations (no entity load)
await evaluator.UpdateRangeAsync(
    new[] { order1, order2, order3 });

// Bulk insert (via EFCore.BulkExtensions)
await evaluator.BulkInsertAsync(
    largeOrderCollection,
    new BulkConfig { BatchSize = 10000 });

// Upsert
var order = await evaluator.UpsertAsync(
    new Order { Id = 123, Status = "Active" },
    keySelector: x => x.Id);

// Delete by condition (no load)
await evaluator.DeleteByConditionAsync(spec);

// Transaction
await evaluator.ExecuteTransactionAsync(async () =>
{
    await evaluator.AddAsync(order);
    await evaluator.DeleteByConditionAsync(oldSpec);
});
```

---

## Specifications

### BasicSpecification<T>

Minimal spec: filter + includes + EF Core hints.

```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(filter)
    .AddInclude(x => x.Customer)
    .WithAsNoTracking(true)
    .AsSplitQuery(true)
    .IgnoreQueryFilters(true);
```

### QuerySpecification<T>

Full spec: filter + includes + ordering + pagination + hints.

```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(filter)
    .WithOrderBy(x => x.CreatedAt)
    .AddThenBy(x => x.Id)
    .WithPagination(page: 1, pageSize: 50)
    .AddInclude(x => x.Customer)
    .WithAsNoTracking(true);
```

---

## EF Core Integration

### Eager Loading

```csharp
spec.AddInclude(x => x.Customer)
    .AddInclude(x => x.LineItems)
    .ThenInclude(x => x.Product);
```

### Query Hints

```csharp
spec
    .WithAsNoTracking(true)       // Disable change tracking
    .AsSplitQuery(true)           // Split includes into separate queries
    .IgnoreQueryFilters(true);    // Bypass global query filters (soft delete, etc)
```

### Filter Negation

```csharp
// Evaluate NOT (filter)
var inactiveOrders = await evaluator.EvaluateQueryAsync(
    spec, 
    negateCondition: true);
```

---

## Performance Tips

1. **Use `QuerySpecification`** for complex queries — ordering and pagination optimize LINQ-to-SQL translation
2. **Enable `AsSplitQuery`** for multiple includes — prevents cartesian explosion
3. **Use `Upsert`** instead of load-then-update for bulk operations
4. **Bulk operations** for large datasets — `BulkInsert`, `BulkUpdate`, `BulkDelete` delegate to EFCore.BulkExtensions (10-100x faster)
5. **Deferred saves** — pass `saveChanges: false` to batch operations, then call `SaveChangesAsync()` once

```csharp
// Good: batch with single save
var orders = new[] { order1, order2, order3 };
await evaluator.AddRangeAsync(orders, saveChanges: false);
await evaluator.SaveChangesAsync();

// Better: bulk for 1000+
await evaluator.BulkInsertAsync(largeCollection);
```

---

## Composable Filters

Filters are composable — build complex logic from simple pieces:

```csharp
var baseFilter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active");

var recentAndHighValue = new ValiFlow<Order>()
    .IsAfter(x => x.CreatedAt, DateTime.UtcNow.AddDays(-30))
    .GreaterThan(x => x.Total, 1000m);

// Combine in spec
var spec = new QuerySpecification<Order>()
    .WithFilter(baseFilter)
    .WithFilter(recentAndHighValue); // Both applied with AND logic
```

---

## Testing & Caching

For unit tests and caching layers, use **Vali-Flow.InMemory** — same filter, synchronous evaluation against `IEnumerable<T>`:

```csharp
// Same filter, no database
var inMemoryEvaluator = new ValiFlowEvaluator<Order, int>(
    cachedOrders, 
    filter, 
    x => x.Id);

var filtered = inMemoryEvaluator.EvaluateAll<DateTime>(
    orderBy: x => x.CreatedAt);
```

---

## Full Ecosystem

- **Vali-Flow.Core** — expression builder (`ValiFlow<T>`)
- **Vali-Flow** — EF Core async evaluator (this package)
- **Vali-Flow.InMemory** — synchronous in-memory evaluator
- **Vali-Flow.Sql** — parameterized SQL translator (Dapper/ADO.NET)
- **Vali-Flow.NoSql.\*** — MongoDB, Elasticsearch, Redis, DynamoDB adapters

See [main repository](https://github.com/UBF21/vali-flow) for complete docs.

---

## License

MIT
