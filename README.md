# Vali-Flow

[![NuGet Version](https://img.shields.io/nuget/v/Vali-Flow?label=Vali-Flow&color=blue)](https://www.nuget.org/packages/Vali-Flow)
[![NuGet Version](https://img.shields.io/nuget/v/Vali-Flow.InMemory?label=Vali-Flow.InMemory&color=blue)](https://www.nuget.org/packages/Vali-Flow.InMemory)
[![NuGet Version](https://img.shields.io/nuget/v/Vali-Flow.Core?label=Vali-Flow.Core&color=blue)](https://www.nuget.org/packages/Vali-Flow.Core)
[![.NET](https://img.shields.io/badge/.NET-7%20%7C%208%20%7C%209-512BD4)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/license-Apache--2.0-green)](LICENSE)

A .NET library that simplifies data access in Entity Framework Core applications through a fluent specification API. Build reusable, composable query criteria and execute them via a single evaluator — without scattering filter logic across repositories.

---

## Features

- Fluent specification classes (`BasicSpecification<T>`, `QuerySpecification<T>`) for encapsulating filter, ordering, and pagination logic
- `ValiFlowEvaluator<T>` for all read and write operations against an EF Core `DbContext`
- Ordering with primary `WithOrderBy` and secondary `AddThenBy` expressions
- Pagination via `WithPagination`, `WithPage`, `WithPageSize`, and `WithTop`
- Eager loading with strongly-typed `AddInclude`
- EF Core query hints: `AsNoTracking`, `AsSplitQuery`, `IgnoreQueryFilters`
- Aggregate queries: `Min`, `Max`, `Average`, `Sum`, grouped variants
- Distinct and duplicate detection queries
- Bulk operations via `EFCore.BulkExtensions`: `BulkInsertAsync`, `BulkUpdateAsync`, `BulkDeleteAsync`, `BulkInsertOrUpdateAsync`
- Upsert support: single entity (`UpsertAsync`) and collection (`UpsertRangeAsync`)
- Transaction support via `ExecuteTransactionAsync`
- `Vali-Flow.InMemory` package for in-memory evaluation without a database (unit testing, caching)
- Built on top of `Vali-Flow.Core` expression builder — zero additional dependencies for filter construction

---

## Installation

Install the EF Core package for production data access:

```bash
dotnet add package Vali-Flow
```

Install the in-memory package for testing or in-process evaluation:

```bash
dotnet add package Vali-Flow.InMemory
```

The `Vali-Flow.Core` expression builder is automatically included as a transitive dependency of both packages.

---

## Quick Start

```csharp
// 1. Create the evaluator (one per entity type, or wrap in a service)
var evaluator = new ValiFlowEvaluator<Order>(dbContext);

// 2. Define a specification
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .EqualTo(x => x.Status, "Active")
        .GreaterThan(x => x.Total, 0m))
    .WithAsNoTracking(true);

// 3. Query
bool hasOrders   = await evaluator.EvaluateAnyAsync(spec, cancellationToken);
int  totalOrders = await evaluator.EvaluateCountAsync(spec, cancellationToken);
Order? first     = await evaluator.EvaluateGetFirstAsync(spec, cancellationToken);
```

---

## Core Concepts

### BasicSpecification\<T\>

Use `BasicSpecification<T>` when you need filtering, eager loading, and EF Core query options, but no ordering or pagination.

```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>()
        .EqualTo(x => x.IsAvailable, true)
        .GreaterThanOrEqualTo(x => x.Stock, 1))
    .AddInclude(x => x.Category)
    .WithAsNoTracking(true)
    .WithAsSplitQuery(false)
    .WithIgnoreQueryFilters(false);
```

`BasicSpecification<T>` can also be constructed directly:

```csharp
var spec = new BasicSpecification<Product>(
    filter: new ValiFlow<Product>().EqualTo(x => x.IsAvailable, true),
    asNoTracking: true,
    asSplitQuery: false,
    ignoreQueryFilters: false
);
```

### QuerySpecification\<T\>

Extends `BasicSpecification<T>` with ordering, pagination, and top-N support. Use this for any query that returns a list.

```csharp
var querySpec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .EqualTo(x => x.CustomerId, customerId)
        .EqualTo(x => x.Status, "Active"))
    .WithOrderBy(x => x.CreatedAt, ascending: false)
    .AddThenBy(x => x.Total, ascending: false)
    .WithPagination(page: 1, pageSize: 20)
    .WithAsNoTracking(true);
```

---

## Reading Data

All read methods are on `ValiFlowEvaluator<T>` and accept a specification as their first argument.

### Existence and count

```csharp
bool exists = await evaluator.EvaluateAnyAsync(spec, cancellationToken);
int  count  = await evaluator.EvaluateCountAsync(spec, cancellationToken);
```

### Single entity

```csharp
Order? first = await evaluator.EvaluateGetFirstAsync(spec, cancellationToken);
Order? last  = await evaluator.EvaluateGetLastAsync(spec, cancellationToken);

// Retrieve the first entity that does NOT match the specification
Order? firstFailed = await evaluator.EvaluateGetFirstFailedAsync(spec, cancellationToken);
Order? lastFailed  = await evaluator.EvaluateGetLastFailedAsync(spec, cancellationToken);
```

### Query (IQueryable)

```csharp
IQueryable<Order> query       = await evaluator.EvaluateQueryAsync(querySpec);
IQueryable<Order> failedQuery = await evaluator.EvaluateQueryFailedAsync(querySpec);

// Project or further compose the queryable before materialising
var totals = await query.Select(o => o.Total).ToListAsync(cancellationToken);
```

### Distinct and duplicates

```csharp
// One entity per CustomerId
IQueryable<Order> distinct = await evaluator.EvaluateDistinctAsync(
    querySpec,
    selector: x => x.CustomerId,
    cancellationToken);

// All orders where the CustomerId appears more than once
IQueryable<Order> duplicates = await evaluator.EvaluateDuplicatesAsync(
    querySpec,
    selector: x => x.CustomerId,
    cancellationToken);
```

### Aggregates

```csharp
decimal minTotal = await evaluator.EvaluateMinAsync(spec, x => x.Total, cancellationToken);
decimal maxTotal = await evaluator.EvaluateMaxAsync(spec, x => x.Total, cancellationToken);
decimal avg      = await evaluator.EvaluateAverageAsync(spec, x => x.Total, cancellationToken);
decimal sum      = await evaluator.EvaluateSumAsync(spec, x => x.Total, cancellationToken);
```

### Grouped aggregates

```csharp
Dictionary<string, List<Order>> byStatus =
    await evaluator.EvaluateGroupedAsync(spec, x => x.Status, cancellationToken);

Dictionary<string, int> countByStatus =
    await evaluator.EvaluateCountByGroupAsync(spec, x => x.Status, cancellationToken);

Dictionary<string, decimal> sumByStatus =
    await evaluator.EvaluateSumByGroupAsync(spec, x => x.Status, x => x.Total, cancellationToken);
```

### Validate a single entity in memory

```csharp
var filter = new ValiFlow<Order>().GreaterThan(x => x.Total, 100m);
bool passes = await evaluator.EvaluateAsync(filter, order);
```

---

## Writing Data

### Single entity

```csharp
Order added   = await evaluator.AddAsync(newOrder, saveChanges: true, cancellationToken);
Order updated = await evaluator.UpdateAsync(order, saveChanges: true, cancellationToken);
await evaluator.DeleteAsync(order, saveChanges: true, cancellationToken);
```

### Collections

```csharp
IEnumerable<Order> added   = await evaluator.AddRangeAsync(orders, cancellationToken: cancellationToken);
IEnumerable<Order> updated = await evaluator.UpdateRangeAsync(orders, cancellationToken: cancellationToken);
await evaluator.DeleteRangeAsync(orders, cancellationToken: cancellationToken);
```

### Delete by condition

```csharp
await evaluator.DeleteByConditionAsync(
    condition: x => x.Status == "Cancelled" && x.CreatedAt < DateTime.UtcNow.AddDays(-30),
    cancellationToken: cancellationToken);
```

### Upsert

```csharp
// Single entity — insert if no match found, update otherwise
Order upserted = await evaluator.UpsertAsync(
    entity: order,
    matchCondition: x => x.Id == order.Id,
    cancellationToken: cancellationToken);

// Collection — matched by key selector
IEnumerable<Order> upserted = await evaluator.UpsertRangeAsync(
    entities: orders,
    keySelector: x => x.Id,
    cancellationToken: cancellationToken);
```

### Deferred saves

Pass `saveChanges: false` to batch multiple operations before committing:

```csharp
await evaluator.AddAsync(order1, saveChanges: false, cancellationToken);
await evaluator.UpdateAsync(order2, saveChanges: false, cancellationToken);
await evaluator.SaveChangesAsync(cancellationToken);
```

### Transactions

```csharp
await evaluator.ExecuteTransactionAsync(async () =>
{
    await evaluator.AddAsync(newOrder, saveChanges: false, cancellationToken);
    await evaluator.UpdateAsync(existingOrder, saveChanges: false, cancellationToken);
    await evaluator.SaveChangesAsync(cancellationToken);
}, cancellationToken);
```

---

## Bulk Operations

Bulk methods use `EFCore.BulkExtensions` for high-throughput scenarios. Pass an optional `BulkConfig` to control batch size, identity output, and which properties to compare or update.

```csharp
var bulkConfig = new BulkConfig { BatchSize = 1000, SetOutputIdentity = true };

await evaluator.BulkInsertAsync(orders, bulkConfig, cancellationToken);
await evaluator.BulkUpdateAsync(orders, bulkConfig, cancellationToken);
await evaluator.BulkDeleteAsync(orders, bulkConfig, cancellationToken);
await evaluator.BulkInsertOrUpdateAsync(orders, bulkConfig, cancellationToken);
```

---

## Ordering and Pagination

### Primary and secondary ordering

```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(filter)
    .WithOrderBy(x => x.CreatedAt, ascending: false)
    .AddThenBy(x => x.Total, ascending: true)
    .AddThenBy(x => x.Id, ascending: true);
```

### Pagination

```csharp
// Fluent chaining
var spec = new QuerySpecification<Order>()
    .WithFilter(filter)
    .WithOrderBy(x => x.CreatedAt, ascending: false)
    .WithPagination(page: 2, pageSize: 25);

// Or set page and page size separately
spec.WithPage(2).WithPageSize(25);
```

### Top-N

```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(filter)
    .WithOrderBy(x => x.Total, ascending: false)
    .WithTop(10); // returns at most 10 records
```

---

## Includes (Eager Loading)

Add navigation properties to be loaded alongside the main entity. `AddInclude` is strongly typed and supports both single entities and collections.

```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().EqualTo(x => x.Status, "Active"))
    .AddInclude(x => x.Customer)
    .AddInclude(x => x.OrderLines)
    .AddInclude(x => x.ShippingAddress);
```

---

## EF Core Query Options

These options can be applied to both `BasicSpecification<T>` and `QuerySpecification<T>`.

| Method | Default | Effect |
|---|---|---|
| `WithAsNoTracking(bool)` | `true` | Disables EF Core change tracking for read-only queries |
| `WithAsSplitQuery(bool)` | `false` | Splits JOIN queries into multiple round-trips to avoid Cartesian explosion |
| `WithIgnoreQueryFilters(bool)` | `false` | Bypasses global query filters (e.g., soft-delete, multi-tenant) |

```csharp
// Retrieve soft-deleted records by bypassing the global IsDeleted filter
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().EqualTo(x => x.CustomerId, customerId))
    .WithIgnoreQueryFilters(true)
    .WithAsNoTracking(true);
```

---

## In-Memory Evaluator

`Vali-Flow.InMemory` provides `ValiFlowEvaluator<T, TProperty>` — a synchronous, dependency-free evaluator that operates on `IEnumerable<T>`. It is ideal for unit tests, in-process caching layers, or scenarios where a database is not available.

### Setup

```csharp
// TProperty is the type of the entity's identity key
var evaluator = new ValiFlowEvaluator<Order, Guid>(
    initialData: orders,
    valiFlow: null,
    getId: x => x.Id);
```

### Reading

```csharp
var filter = new ValiFlow<Order>().EqualTo(x => x.Status, "Active");

bool any   = evaluator.EvaluateAny(orders, filter);
int  count = evaluator.EvaluateCount(orders, filter);

Order? first = evaluator.GetFirst(orders, filter);
Order? last  = evaluator.GetLast(orders, filter);

IEnumerable<Order> all   = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    ascending: false,
    valiFlow: filter);

IEnumerable<Order> paged = evaluator.EvaluatePaged<DateTime>(
    orders,
    page: 1,
    pageSize: 10,
    orderBy: x => x.CreatedAt,
    ascending: false,
    valiFlow: filter);

IEnumerable<Order> top5 = evaluator.EvaluateTop<decimal>(
    orders,
    count: 5,
    orderBy: x => x.Total,
    ascending: false,
    valiFlow: filter);
```

### Distinct and duplicates

```csharp
IEnumerable<Order> distinct = evaluator.EvaluateDistinct<Guid>(
    orders,
    selector: x => x.CustomerId,
    orderBy: x => x.CreatedAt,
    ascending: false,
    valiFlow: filter);

IEnumerable<Order> duplicates = evaluator.EvaluateDuplicates<Guid>(
    orders,
    selector: x => x.CustomerId,
    valiFlow: filter);
```

### Aggregates

```csharp
decimal min = evaluator.EvaluateMin(orders, x => x.Total, filter);
decimal max = evaluator.EvaluateMax(orders, x => x.Total, filter);
decimal avg = evaluator.EvaluateAverage(orders, x => x.Total, filter);
decimal sum = evaluator.EvaluateSum(orders, x => x.Total, filter);
```

### Grouped operations

```csharp
Dictionary<string, List<Order>> grouped =
    evaluator.EvaluateGrouped(orders, x => x.Status, filter);

Dictionary<string, int> countByStatus =
    evaluator.EvaluateCountByGroup(orders, x => x.Status, filter);

Dictionary<string, decimal> sumByStatus =
    evaluator.EvaluateSumByGroup(orders, x => x.Status, x => x.Total, filter);

Dictionary<string, decimal> avgByStatus =
    evaluator.EvaluateAverageByGroup(orders, x => x.Status, x => x.Total, filter);

Dictionary<string, List<Order>> topByStatus =
    evaluator.EvaluateTopByGroup(orders, x => x.Status, count: 3, orderBy: x => x.Total, filter);
```

### Writing

```csharp
evaluator.Add(newOrder, orders);
evaluator.Update(existingOrder, orders);
evaluator.Delete(existingOrder, orders);

evaluator.AddRange(newOrders, orders);
IEnumerable<Order> updated = evaluator.UpdateRange(modifiedOrders, orders);
int deleted = evaluator.DeleteRange(staleOrders, orders);

evaluator.SaveChanges(orders);
```

### Negating conditions

Every read method accepts a `negateCondition` parameter. When set to `true`, the filter is inverted — equivalent to a logical NOT of the `ValiFlow<T>` expression.

```csharp
// All orders that do NOT have Status == "Active"
IEnumerable<Order> inactive = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    valiFlow: new ValiFlow<Order>().EqualTo(x => x.Status, "Active"),
    negateCondition: true);
```

---

## Integration with Vali-Flow.Core

Both `Vali-Flow` and `Vali-Flow.InMemory` use `Vali-Flow.Core` to build `Expression<Func<T, bool>>` trees. The `ValiFlow<T>` builder supports a wide range of predicates:

```csharp
var filter = new ValiFlow<Order>()
    // Comparison
    .EqualTo(x => x.Status, "Active")
    .NotEqualTo(x => x.Status, "Cancelled")
    .GreaterThan(x => x.Total, 0m)
    .LessThanOrEqualTo(x => x.Total, 10_000m)
    // String
    .Contains(x => x.Reference, "ORD")
    .StartsWith(x => x.Reference, "2025")
    .HasMinLength(x => x.Reference, 5)
    // Numeric range
    .Between(x => x.Quantity, 1, 100)
    // Collection
    .NotEmpty(x => x.OrderLines)
    // DateTime
    .IsAfter(x => x.CreatedAt, DateTime.UtcNow.AddDays(-30))
    // Boolean
    .IsTrue(x => x.IsConfirmed)
    // Logical operators
    .Or()
    .EqualTo(x => x.Status, "Pending");
```

Refer to the [Vali-Flow.Core repository](https://github.com/UBF21/vali-flow) for the full list of available predicates.

---

## License

Licensed under the [Apache 2.0 License](LICENSE).
Copyright &copy; 2025 Felipe Rafael Montenegro Morriberon.
