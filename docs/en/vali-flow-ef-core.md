# Vali-Flow — EF Core Evaluator

## Table of Contents

1. [Overview](#overview)
2. [Setup](#setup)
3. [Specifications](#specifications)
   - [BasicSpecification\<T\>](#basicspecificationt)
   - [QuerySpecification\<T\>](#queryspecificationt)
4. [Read Methods](#read-methods)
   - [negateCondition explained](#negatecondition-explained)
   - [EvaluateAsync](#evaluateasync)
   - [EvaluateAnyAsync](#evaluateanyasync)
   - [EvaluateCountAsync](#evaluatecountasync)
   - [EvaluateGetFirstAsync](#evaluategetfirstasync)
   - [EvaluateGetLastAsync](#evaluategetlastasync)
   - [EvaluateGetFirstFailedAsync](#evaluategetfirstfailedasync)
   - [EvaluateGetLastFailedAsync](#evaluategetlastfailedasync)
   - [EvaluateQueryAsync](#evaluatequeryasync)
   - [EvaluateQueryFailedAsync](#evaluatequeryfailedasync)
   - [EvaluateAllAsync](#evaluateallasync)
   - [EvaluateAllFailedAsync](#evaluateallfailedasync)
   - [EvaluateTopAsync](#evaluatetopasync)
   - [EvaluateDistinctAsync](#evaluatedistinctasync)
   - [EvaluateDuplicatesAsync](#evaluateduplicatesasync)
   - [EvaluatePagedAsync](#evaluatepagedasync)
   - [EvaluateMinAsync](#evaluateminasync)
   - [EvaluateMaxAsync](#evaluatemaxasync)
   - [EvaluateAverageAsync](#evaluateaverageasync)
   - [EvaluateSumAsync](#evaluatesumasync)
   - [EvaluateAggregateAsync](#evaluateaggregateasync)
   - [EvaluateGroupedAsync](#evaluategroupedasync)
   - [EvaluateCountByGroupAsync](#evaluatecountbygroupasync)
   - [EvaluateSumByGroupAsync](#evaluatesumbygroupasync)
   - [EvaluateMinByGroupAsync](#evaluateminbygroupasync)
   - [EvaluateMaxByGroupAsync](#evaluatemaxbygroupasync)
   - [EvaluateAverageByGroupAsync](#evaluateaveragebygroupasync)
   - [EvaluateDuplicatesByGroupAsync](#evaluateduplicatesbygroupasync)
   - [EvaluateUniquesByGroupAsync](#evaluateuniquesbygroupasync)
   - [EvaluateTopByGroupAsync](#evaluatetopbygroupasync)
5. [Write Methods](#write-methods)
   - [AddAsync](#addasync)
   - [AddRangeAsync](#addrangeasync)
   - [UpdateAsync](#updateasync)
   - [UpdateRangeAsync](#updaterangeasync)
   - [DeleteAsync](#deleteasync)
   - [DeleteRangeAsync](#deleterangeasync)
   - [DeleteByConditionAsync](#deletebyconditionasync)
   - [SaveChangesAsync](#savechangesasync)
   - [UpsertAsync](#upsertasync)
   - [UpsertRangeAsync](#upsertrangeasync)
   - [ExecuteTransactionAsync](#executetransactionasync)
   - [ExecuteUpdateAsync](#executeupdateasync)
   - [BulkInsertAsync](#bulkinsertasync)
   - [BulkUpdateAsync](#bulkupdateasync)
   - [BulkDeleteAsync](#bulkdeleteasync)
   - [BulkInsertOrUpdateAsync](#bulkinserorupdateasync)

---

## Overview

**Vali-Flow** is a .NET library that simplifies data access in Entity Framework Core applications through a fluent specification API. It decouples query criteria from the data access layer, producing clean, composable, and testable code.

Key features:

- Fluent specification pattern with filter, ordering, pagination, and EF Core hints
- Full async API built on top of EF Core
- Bulk operations via `EFCore.BulkExtensions`
- Deferred saves for batching multiple write operations
- Inverted filters with `negateCondition` without changing the specification

**Install**

```bash
dotnet add package Vali-Flow
```

**Requirements**

- .NET 8 or later
- Entity Framework Core 8 or later

---

## Setup

`ValiFlowEvaluator<T>` is a sealed generic class that accepts a `DbContext` and implements both `IEvaluatorRead<T>` and `IEvaluatorWrite<T>`.

**Constructor**

```csharp
public ValiFlowEvaluator<T>(DbContext dbContext)
```

**Dependency Injection (recommended)**

Register a typed evaluator per entity in your composition root:

```csharp
// Program.cs / Startup.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<ValiFlowEvaluator<Order>>(sp =>
    new ValiFlowEvaluator<Order>(sp.GetRequiredService<AppDbContext>()));
```

**Manual instantiation**

```csharp
var evaluator = new ValiFlowEvaluator<Order>(dbContext);
```

> **Note:** The evaluator holds a reference to the `DbContext` provided. Follow standard EF Core scoping rules — inject a scoped `DbContext` in web applications.

---

## Specifications

Specifications encapsulate all query criteria in a single reusable object. There are two specification classes:

| Class | Inherits | Use when |
|-------|----------|----------|
| `BasicSpecification<T>` | — | Filtering, includes, EF hints only |
| `QuerySpecification<T>` | `BasicSpecification<T>` | Ordering, pagination, top, or ValiSort |

### BasicSpecification\<T\>

`BasicSpecification<T>` is the base class. Configure it using its fluent methods:

| Method | Description |
|--------|-------------|
| `WithFilter(ValiFlow<T>)` | Sets the filter expression |
| `WithIncludes(...)` | Adds eager-load includes |
| `AsNoTracking()` | Disables EF Core change tracking (default: enabled) |
| `AsSplitQuery()` | Enables split queries for collections |
| `IgnoreQueryFilters()` | Bypasses global query filters |

```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .AsNoTracking();
```

### QuerySpecification\<T\>

`QuerySpecification<T>` extends `BasicSpecification<T>` with:

| Method | Description |
|--------|-------------|
| `WithOrderBy<TProperty>(expr, ascending)` | Primary ordering |
| `AddThenBy<TProperty>(expr, ascending)` | Secondary ordering (requires `WithOrderBy` first) |
| `AddThenBys<TProperty>(exprs, ascending)` | Multiple secondary orderings at once |
| `WithPagination(page, pageSize)` | Sets page and page size together |
| `WithPage(page)` | Sets page number only |
| `WithPageSize(pageSize)` | Sets page size only |
| `WithTop(count)` | Limits result to the first N items |
| `WithValiSort(ValiSort<T>)` | Dynamic ordering via reflection |

**Mutual exclusion rules**

- `WithOrderBy` and `WithValiSort` cannot be combined.
- `WithTop` and `WithPagination` / `WithPage` / `WithPageSize` cannot be combined.
- `AddThenBy` requires `WithOrderBy` to be called first.

**Example — combined spec**

```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .IsTrue(o => o.IsActive)
        .GreaterThan(o => o.Total, 500m))
    .WithOrderBy(o => o.CreatedAt, ascending: false)
    .AddThenBy(o => o.CustomerId)
    .WithPagination(page: 2, pageSize: 20)
    .AsNoTracking();
```

**Example — constructor with filter**

```csharp
var filter = new ValiFlow<Product>().LessThan(p => p.Stock, 10);
var spec = new QuerySpecification<Product>(filter)
    .WithOrderBy(p => p.Name)
    .WithTop(5);
```

**Example — ValiSort (dynamic sorting)**

```csharp
var sort = new ValiSort<Order>().By("Total", ascending: false);
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithValiSort(sort);
```

---

## Read Methods

All read methods are asynchronous and operate on the `DbContext` set for entity type `T`. Specifications are passed by interface (`IBasicSpecification<T>` or `IQuerySpecification<T>`), so both `BasicSpecification<T>` and `QuerySpecification<T>` are accepted where the base interface is expected.

### negateCondition explained

Several read methods accept a `negateCondition` parameter (implicit through "Failed" method variants or explicit through query building). When the underlying query is built with the negated filter, the `ValiFlow<T>` expression is logically inverted — entities that would normally be **excluded** become **included**, and vice versa.

This allows you to reuse the same specification to query both passing and failing entities:

```csharp
var activeFilter = new ValiFlow<User>().IsTrue(u => u.IsActive);
var spec = new BasicSpecification<User>().WithFilter(activeFilter);

// Returns active users
var active = await evaluator.EvaluateAnyAsync(spec);

// The "Failed" variants automatically negate the filter
var firstInactive = await evaluator.EvaluateGetFirstFailedAsync(spec);
```

---

### `EvaluateAsync`

**Signature**
```csharp
Task<bool> EvaluateAsync(ValiFlow<T> valiFlow, T entity)
```

**Description**
Evaluates whether a single in-memory entity satisfies the given `ValiFlow<T>` condition. Does not hit the database. Useful for validating entities before saving.

**Example**
```csharp
var rule = new ValiFlow<Order>()
    .GreaterThan(o => o.Total, 0m)
    .IsNotNull(o => o.CustomerId);

var order = new Order { Total = 150m, CustomerId = "C001" };
bool isValid = await evaluator.EvaluateAsync(rule, order);
```

> **Note:** This is the only read method that does not query the database.

---

### `EvaluateAnyAsync`

**Signature**
```csharp
Task<bool> EvaluateAnyAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default)
```

**Description**
Returns `true` if at least one entity in the database satisfies the specification's filter. Translates to a SQL `EXISTS` or `COUNT` check.

**Example**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().LessThan(p => p.Stock, 5));

bool hasLowStock = await evaluator.EvaluateAnyAsync(spec);
```

---

### `EvaluateCountAsync`

**Signature**
```csharp
Task<int> EvaluateCountAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default)
```

**Description**
Returns the total number of entities matching the specification's filter.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .Equal(o => o.Status, OrderStatus.Pending));

int pendingCount = await evaluator.EvaluateCountAsync(spec);
```

---

### `EvaluateGetFirstAsync`

**Signature**
```csharp
Task<T?> EvaluateGetFirstAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default)
```

**Description**
Returns the first entity matching the specification, or `null` if none match. Equivalent to `FirstOrDefaultAsync` with the applied filter.

**Example**
```csharp
var spec = new BasicSpecification<User>()
    .WithFilter(new ValiFlow<User>().Equal(u => u.Email, "admin@example.com"));

User? admin = await evaluator.EvaluateGetFirstAsync(spec);
```

---

### `EvaluateGetLastAsync`

**Signature**
```csharp
Task<T?> EvaluateGetLastAsync(IQuerySpecification<T> specification, CancellationToken cancellationToken = default)
```

**Description**
Returns the last entity matching the specification according to the defined ordering. Returns `null` if no match is found.

**Example**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.CreatedAt);

Order? lastOrder = await evaluator.EvaluateGetLastAsync(spec);
```

> **Note:** A primary ordering (`WithOrderBy`) must be defined. SQL databases have no concept of "last" without `ORDER BY`.

---

### `EvaluateGetFirstFailedAsync`

**Signature**
```csharp
Task<T?> EvaluateGetFirstFailedAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default)
```

**Description**
Returns the first entity that does **not** satisfy the specification's filter, or `null` if all entities pass. Applies the logical NOT of the filter.

**Example**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsAvailable));

Product? unavailable = await evaluator.EvaluateGetFirstFailedAsync(spec);
```

---

### `EvaluateGetLastFailedAsync`

**Signature**
```csharp
Task<T?> EvaluateGetLastFailedAsync(IQuerySpecification<T> specification, CancellationToken cancellationToken = default)
```

**Description**
Returns the last entity that does **not** satisfy the specification's filter, ordered according to the specification. Returns `null` if all entities pass.

**Example**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsPaid))
    .WithOrderBy(o => o.CreatedAt);

Order? lastUnpaid = await evaluator.EvaluateGetLastFailedAsync(spec);
```

---

### `EvaluateQueryAsync`

**Signature**
```csharp
Task<IQueryable<T>> EvaluateQueryAsync(IQuerySpecification<T> specification)
```

**Description**
Returns an `IQueryable<T>` with the full specification applied — filter, ordering, pagination, includes, and EF hints. The query is not yet executed; you can compose further or materialize with `ToListAsync`, `ToArrayAsync`, etc.

**Example**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.CreatedAt, ascending: false)
    .WithPagination(page: 1, pageSize: 10)
    .AsNoTracking();

IQueryable<Order> query = await evaluator.EvaluateQueryAsync(spec);
List<Order> orders = await query.ToListAsync();
```

---

### `EvaluateQueryFailedAsync`

**Signature**
```csharp
Task<IQueryable<T>> EvaluateQueryFailedAsync(IQuerySpecification<T> specification)
```

**Description**
Returns an `IQueryable<T>` for entities that do **not** satisfy the specification's filter, with ordering and pagination applied.

**Example**
```csharp
var spec = new QuerySpecification<User>()
    .WithFilter(new ValiFlow<User>().IsTrue(u => u.EmailConfirmed))
    .WithOrderBy(u => u.CreatedAt);

IQueryable<User> query = await evaluator.EvaluateQueryFailedAsync(spec);
List<User> unconfirmed = await query.ToListAsync();
```

---

### `EvaluateAllAsync`

**Signature**
```csharp
Task<IQueryable<T>> EvaluateAllAsync(IQuerySpecification<T> specification)
```

**Description**
Alias for `EvaluateQueryAsync`. Returns all entities matching the specification. Provided for API symmetry with the `Vali-Flow.InMemory` package.

**Example**
```csharp
var spec = new QuerySpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsActive))
    .WithOrderBy(p => p.Name);

var query = await evaluator.EvaluateAllAsync(spec);
var products = await query.ToListAsync();
```

---

### `EvaluateAllFailedAsync`

**Signature**
```csharp
Task<IQueryable<T>> EvaluateAllFailedAsync(IQuerySpecification<T> specification)
```

**Description**
Alias for `EvaluateQueryFailedAsync`. Returns all entities that fail the specification. Provided for API symmetry with the `Vali-Flow.InMemory` package.

**Example**
```csharp
var spec = new QuerySpecification<Product>()
    .WithFilter(new ValiFlow<Product>().GreaterThan(p => p.Stock, 0))
    .WithOrderBy(p => p.Name);

var query = await evaluator.EvaluateAllFailedAsync(spec);
var outOfStock = await query.ToListAsync();
```

---

### `EvaluateTopAsync`

**Signature**
```csharp
Task<IQueryable<T>> EvaluateTopAsync(IQuerySpecification<T> specification, int count, CancellationToken cancellationToken = default)
```

**Description**
Returns an `IQueryable<T>` limited to the first `count` entities that satisfy the specification, with ordering applied.

**Example**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.Total, ascending: false);

IQueryable<Order> topOrders = await evaluator.EvaluateTopAsync(spec, count: 5);
var result = await topOrders.ToListAsync();
```

> **Note:** `count` must be greater than zero.

---

### `EvaluateDistinctAsync`

**Signature**
```csharp
Task<IEnumerable<T>> EvaluateDistinctAsync<TKey>(
    IQuerySpecification<T> specification,
    Expression<Func<T, TKey>> selector
) where TKey : notnull
```

**Description**
Returns a distinct set of entities grouped by the provided key selector. For each group, only the first entity is returned. Ordering and pagination from the specification are applied after deduplication.

**Example**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.CreatedAt);

// One order per customer
IEnumerable<Order> distinctOrders = await evaluator.EvaluateDistinctAsync(
    spec, o => o.CustomerId);
```

---

### `EvaluateDuplicatesAsync`

**Signature**
```csharp
Task<IEnumerable<T>> EvaluateDuplicatesAsync<TKey>(
    IQuerySpecification<T> specification,
    Expression<Func<T, TKey>> selector
) where TKey : notnull
```

**Description**
Returns all entities belonging to groups where the key appears more than once — i.e., duplicate entries by the given key.

**Example**
```csharp
var spec = new QuerySpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsActive));

// Products sharing the same SKU
IEnumerable<Product> duplicates = await evaluator.EvaluateDuplicatesAsync(
    spec, p => p.Sku);
```

---

### `EvaluatePagedAsync`

**Signature**
```csharp
Task<PagedResult<T>> EvaluatePagedAsync(
    IQuerySpecification<T> specification,
    CancellationToken cancellationToken = default
)
```

**Description**
Executes the query and returns a `PagedResult<T>` containing the items for the requested page along with pagination metadata (total count, total pages, current page, page size).

**Example**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.CreatedAt, ascending: false)
    .WithPagination(page: 1, pageSize: 20)
    .AsNoTracking();

PagedResult<Order> paged = await evaluator.EvaluatePagedAsync(spec);

Console.WriteLine($"Page {paged.Page} of {paged.TotalPages} ({paged.TotalCount} total)");
foreach (var order in paged.Items)
{
    Console.WriteLine(order.Id);
}
```

---

### `EvaluateMinAsync`

**Signature**
```csharp
Task<TResult> EvaluateMinAsync<TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TResult>> selector,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult>
```

**Description**
Returns the minimum value of the selected numeric property among entities matching the specification.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

decimal minTotal = await evaluator.EvaluateMinAsync(spec, o => o.Total);
```

---

### `EvaluateMaxAsync`

**Signature**
```csharp
Task<TResult> EvaluateMaxAsync<TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TResult>> selector,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult>
```

**Description**
Returns the maximum value of the selected numeric property among entities matching the specification.

**Example**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsAvailable));

decimal maxPrice = await evaluator.EvaluateMaxAsync(spec, p => p.Price);
```

---

### `EvaluateAverageAsync`

**Signature**
```csharp
Task<decimal> EvaluateAverageAsync<TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TResult>> selector,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult>
```

**Description**
Returns the arithmetic average (as `decimal`) of the selected numeric property among entities matching the specification.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsPaid));

decimal avgTotal = await evaluator.EvaluateAverageAsync(spec, o => o.Total);
```

---

### `EvaluateSumAsync`

**Signature**
```csharp
// Overloads for int, long, double, decimal, float
Task<int>     EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, int>>, CancellationToken = default)
Task<long>    EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, long>>, CancellationToken = default)
Task<double>  EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, double>>, CancellationToken = default)
Task<decimal> EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, decimal>>, CancellationToken = default)
Task<float>   EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, float>>, CancellationToken = default)
```

**Description**
Returns the sum of the selected numeric property for all entities matching the specification. Five overloads cover `int`, `long`, `double`, `decimal`, and `float`.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .Equal(o => o.Status, OrderStatus.Completed));

decimal totalRevenue = await evaluator.EvaluateSumAsync(spec, o => o.Total);
int totalItems = await evaluator.EvaluateSumAsync(spec, o => o.ItemCount);
```

---

### `EvaluateAggregateAsync`

**Signature**
```csharp
Task<TResult> EvaluateAggregateAsync<TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TResult>> selector,
    Func<TResult, TResult, TResult> aggregator,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult>
```

**Description**
Applies a custom aggregation function over the selected property values. Useful for aggregations not covered by the built-in Min/Max/Sum/Average methods.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

// Product of all totals (custom aggregation)
decimal product = await evaluator.EvaluateAggregateAsync(
    spec,
    o => o.Total,
    (acc, val) => acc * val);
```

---

### `EvaluateGroupedAsync`

**Signature**
```csharp
Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Description**
Groups all matching entities by the provided key selector and returns a dictionary mapping each key to the list of entities in that group.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

Dictionary<string, List<Order>> byCustomer =
    await evaluator.EvaluateGroupedAsync(spec, o => o.CustomerId);
```

---

### `EvaluateCountByGroupAsync`

**Signature**
```csharp
Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Description**
Returns a dictionary mapping each group key to the count of entities in that group.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

Dictionary<OrderStatus, int> countByStatus =
    await evaluator.EvaluateCountByGroupAsync(spec, o => o.Status);
```

---

### `EvaluateSumByGroupAsync`

**Signature**
```csharp
// Overloads for int, long, float, double, decimal
Task<Dictionary<TKey, int>>     EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, int>>, ct)
Task<Dictionary<TKey, long>>    EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, long>>, ct)
Task<Dictionary<TKey, float>>   EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, float>>, ct)
Task<Dictionary<TKey, double>>  EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, double>>, ct)
Task<Dictionary<TKey, decimal>> EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, decimal>>, ct)
```

**Description**
Groups entities by the key selector and returns a dictionary mapping each key to the sum of the selected numeric property within that group. Five overloads cover the main numeric types.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsPaid));

Dictionary<string, decimal> revenueByCustomer =
    await evaluator.EvaluateSumByGroupAsync(spec, o => o.CustomerId, o => o.Total);
```

---

### `EvaluateMinByGroupAsync`

**Signature**
```csharp
// Overloads for int, long, float, double, decimal
Task<Dictionary<TKey, int>>     EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, int>>, ct)
Task<Dictionary<TKey, long>>    EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, long>>, ct)
Task<Dictionary<TKey, float>>   EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, float>>, ct)
Task<Dictionary<TKey, double>>  EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, double>>, ct)
Task<Dictionary<TKey, decimal>> EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, decimal>>, ct)
```

**Description**
Groups entities by the key selector and returns the minimum value of the selected numeric property per group. Returns `default(TValue)` for empty groups.

**Example**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsActive));

Dictionary<string, decimal> minPriceByCategory =
    await evaluator.EvaluateMinByGroupAsync(spec, p => p.Category, p => p.Price);
```

---

### `EvaluateMaxByGroupAsync`

**Signature**
```csharp
// Overloads for int, long, float, double, decimal
Task<Dictionary<TKey, int>>     EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, int>>, ct)
Task<Dictionary<TKey, long>>    EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, long>>, ct)
Task<Dictionary<TKey, float>>   EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, float>>, ct)
Task<Dictionary<TKey, double>>  EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, double>>, ct)
Task<Dictionary<TKey, decimal>> EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, decimal>>, ct)
```

**Description**
Groups entities by the key selector and returns the maximum value of the selected numeric property per group. Returns `default(TValue)` for empty groups.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

Dictionary<string, decimal> maxOrderByCustomer =
    await evaluator.EvaluateMaxByGroupAsync(spec, o => o.CustomerId, o => o.Total);
```

---

### `EvaluateAverageByGroupAsync`

**Signature**
```csharp
Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    Expression<Func<T, TResult>> selector,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult> where TKey : notnull
```

**Description**
Groups entities by the key selector and returns the average (as `decimal`) of the selected numeric property per group.

**Example**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsPaid));

Dictionary<string, decimal> avgByCustomer =
    await evaluator.EvaluateAverageByGroupAsync(spec, o => o.CustomerId, o => o.Total);
```

---

### `EvaluateDuplicatesByGroupAsync`

**Signature**
```csharp
Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Description**
Returns a dictionary mapping each duplicate key to the list of entities that share it. Only groups with more than one entity are included.

**Example**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsActive));

Dictionary<string, List<Product>> duplicateBySku =
    await evaluator.EvaluateDuplicatesByGroupAsync(spec, p => p.Sku);
```

---

### `EvaluateUniquesByGroupAsync`

**Signature**
```csharp
Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Description**
Returns a dictionary mapping each key to a single unique entity. Only groups with exactly one entity are included — groups with duplicates are excluded.

**Example**
```csharp
var spec = new BasicSpecification<User>()
    .WithFilter(new ValiFlow<User>().IsTrue(u => u.IsActive));

Dictionary<string, User> uniqueByEmail =
    await evaluator.EvaluateUniquesByGroupAsync(spec, u => u.Email);
```

---

### `EvaluateTopByGroupAsync`

**Signature**
```csharp
Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey>(
    IQuerySpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Description**
Retrieves the top N entities (as defined by `WithTop` in the specification) and groups them by the key selector. If `Top` is not set, defaults to 50 entities. Pagination properties are ignored.

**Example**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.Total, ascending: false)
    .WithTop(10);

Dictionary<string, List<Order>> top10ByCustomer =
    await evaluator.EvaluateTopByGroupAsync(spec, o => o.CustomerId);
```

---

## Write Methods

All write methods are asynchronous. Most accept a `saveChanges` parameter (default `true`) which controls whether `SaveChangesAsync` is called immediately. Pass `saveChanges: false` to batch multiple operations and call `SaveChangesAsync()` once at the end.

### Deferred saves pattern

```csharp
var order = new Order { CustomerId = "C001", Total = 250m };
var item1 = new OrderItem { ProductId = "P1", Quantity = 2 };
var item2 = new OrderItem { ProductId = "P2", Quantity = 1 };

await orderEvaluator.AddAsync(order, saveChanges: false);
await itemEvaluator.AddAsync(item1, saveChanges: false);
await itemEvaluator.AddAsync(item2, saveChanges: false);

await orderEvaluator.SaveChangesAsync(); // single round-trip
```

---

### `AddAsync`

**Signature**
```csharp
Task<T> AddAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Description**
Adds a single entity to the database. Returns the added entity (with any database-generated values populated if `saveChanges: true`).

**Example**
```csharp
var product = new Product { Name = "Widget", Price = 9.99m, Stock = 100 };
Product saved = await evaluator.AddAsync(product);
Console.WriteLine(saved.Id); // auto-generated ID is available
```

> **Note:** Throws `ArgumentNullException` if `entity` is null.

---

### `AddRangeAsync`

**Signature**
```csharp
Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Description**
Adds a collection of entities to the database in a single operation. Returns the added entities.

**Example**
```csharp
var products = new List<Product>
{
    new Product { Name = "Widget A", Price = 9.99m },
    new Product { Name = "Widget B", Price = 14.99m },
};

IEnumerable<Product> saved = await evaluator.AddRangeAsync(products);
```

> **Note:** Throws `ArgumentException` if `entities` is empty.

---

### `UpdateAsync`

**Signature**
```csharp
Task<T> UpdateAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Description**
Marks a single entity as modified and persists the changes to the database. The entity must be tracked by EF Core or have its primary key set.

**Example**
```csharp
var order = await evaluator.EvaluateGetFirstAsync(spec);
order.Status = OrderStatus.Shipped;

Order updated = await evaluator.UpdateAsync(order);
```

---

### `UpdateRangeAsync`

**Signature**
```csharp
Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Description**
Marks a collection of entities as modified and persists all changes in one call.

**Example**
```csharp
var orders = (await evaluator.EvaluateQueryAsync(spec)).ToList();
orders.ForEach(o => o.Status = OrderStatus.Processing);

await evaluator.UpdateRangeAsync(orders);
```

---

### `DeleteAsync`

**Signature**
```csharp
Task DeleteAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Description**
Removes a single entity from the database. The entity must be tracked or attached before deletion.

**Example**
```csharp
var product = await evaluator.EvaluateGetFirstAsync(spec);
if (product is not null)
    await evaluator.DeleteAsync(product);
```

---

### `DeleteRangeAsync`

**Signature**
```csharp
Task DeleteRangeAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Description**
Removes a collection of entities from the database in one call.

**Example**
```csharp
var expiredOrders = await (await evaluator.EvaluateQueryAsync(spec)).ToListAsync();
await evaluator.DeleteRangeAsync(expiredOrders);
```

---

### `DeleteByConditionAsync`

**Signature**
```csharp
Task DeleteByConditionAsync(Expression<Func<T, bool>> condition, CancellationToken cancellationToken = default)
```

**Description**
Issues a direct `DELETE` SQL statement for all entities matching the condition without loading entities into memory. Uses EF Core 7+ `ExecuteDeleteAsync`. No `saveChanges` parameter — the delete is applied immediately.

**Example**
```csharp
await evaluator.DeleteByConditionAsync(o => o.CreatedAt < DateTime.UtcNow.AddYears(-2));
```

> **Note:** Does not trigger EF Core change tracking or entity events. Use carefully with soft-delete configurations.

---

### `SaveChangesAsync`

**Signature**
```csharp
Task SaveChangesAsync(CancellationToken cancellationToken = default)
```

**Description**
Flushes all pending changes in the `DbContext` to the database. Use this after batching multiple operations with `saveChanges: false`.

**Example**
```csharp
await evaluator.AddAsync(entity1, saveChanges: false);
await evaluator.UpdateAsync(entity2, saveChanges: false);
await evaluator.SaveChangesAsync();
```

---

### `UpsertAsync`

**Signature**
```csharp
Task<T> UpsertAsync(
    T entity,
    Expression<Func<T, bool>> matchCondition,
    bool saveChanges = true,
    CancellationToken cancellationToken = default)
```

**Description**
Inserts the entity if no existing record matches `matchCondition`; otherwise updates the matched record. Returns the inserted or updated entity.

**Example**
```csharp
var product = new Product { Sku = "SKU-001", Name = "Widget", Price = 9.99m };

Product result = await evaluator.UpsertAsync(
    product,
    matchCondition: p => p.Sku == product.Sku);
```

---

### `UpsertRangeAsync`

**Signature**
```csharp
Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
    IEnumerable<T> entities,
    Expression<Func<T, TProperty>> keySelector,
    bool saveChanges = true,
    CancellationToken cancellationToken = default
) where TProperty : notnull
```

**Description**
Inserts or updates a collection of entities using the key selector to match existing records. Returns all inserted or updated entities.

**Example**
```csharp
var products = new List<Product>
{
    new Product { Sku = "SKU-001", Name = "Widget A", Price = 9.99m },
    new Product { Sku = "SKU-002", Name = "Widget B", Price = 14.99m },
};

await evaluator.UpsertRangeAsync(products, keySelector: p => p.Sku);
```

---

### `ExecuteTransactionAsync`

**Signature**
```csharp
Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default)
```

**Description**
Executes the provided delegate inside a database transaction. If any operation throws, the transaction is rolled back automatically.

**Example**
```csharp
await evaluator.ExecuteTransactionAsync(async () =>
{
    await orderEvaluator.AddAsync(newOrder, saveChanges: false);
    await inventoryEvaluator.UpdateAsync(updatedInventory, saveChanges: false);
    await orderEvaluator.SaveChangesAsync();
});
```

> **Note:** Throws `InvalidOperationException` if the transaction or its rollback fails.

---

### `ExecuteUpdateAsync`

**Signature**
```csharp
Task<int> ExecuteUpdateAsync(
    Expression<Func<T, bool>> condition,
    Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls,
    CancellationToken cancellationToken = default)
```

**Description**
Issues a direct `UPDATE` SQL statement for all entities matching `condition` without loading them into memory. Uses EF Core 7+ `ExecuteUpdateAsync`. Returns the number of rows affected.

**Example**
```csharp
int affected = await evaluator.ExecuteUpdateAsync(
    condition: o => o.Status == OrderStatus.Pending && o.CreatedAt < DateTime.UtcNow.AddDays(-30),
    setPropertyCalls: s => s
        .SetProperty(o => o.Status, OrderStatus.Cancelled)
        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));
```

> **Note:** Does not trigger EF Core change tracking or entity lifecycle events.

---

### `BulkInsertAsync`

**Signature**
```csharp
Task BulkInsertAsync(IEnumerable<T> entities, BulkConfig? bulkConfig = null, CancellationToken cancellationToken = default)
```

**Description**
Inserts a large collection of entities in a single high-performance bulk operation using `EFCore.BulkExtensions`. Significantly faster than `AddRangeAsync` + `SaveChangesAsync` for thousands of records.

`BulkConfig` options of interest:
- `BatchSize` — controls the number of rows per database round-trip
- `SetOutputIdentity` — retrieves auto-generated primary keys after insert
- `IncludeGraph` — includes related entities in the insert

**Example**
```csharp
var products = Enumerable.Range(1, 10_000)
    .Select(i => new Product { Name = $"Product {i}", Price = i * 1.5m })
    .ToList();

var config = new BulkConfig { BatchSize = 1000, SetOutputIdentity = true };
await evaluator.BulkInsertAsync(products, config);
```

---

### `BulkUpdateAsync`

**Signature**
```csharp
Task BulkUpdateAsync(IEnumerable<T> entities, BulkConfig? bulkConfig = null, CancellationToken cancellationToken = default)
```

**Description**
Updates a large collection of entities in a single bulk operation. Entities must have valid primary keys.

`BulkConfig` options of interest:
- `BatchSize` — rows per round-trip
- `PropertiesToInclude` — limits which columns are updated

**Example**
```csharp
var products = await (await evaluator.EvaluateQueryAsync(spec)).ToListAsync();
products.ForEach(p => p.Price *= 1.1m);

var config = new BulkConfig
{
    BatchSize = 500,
    PropertiesToInclude = new List<string> { nameof(Product.Price) }
};
await evaluator.BulkUpdateAsync(products, config);
```

---

### `BulkDeleteAsync`

**Signature**
```csharp
Task BulkDeleteAsync(IEnumerable<T> entities, BulkConfig? bulkConfig = null, CancellationToken cancellationToken = default)
```

**Description**
Deletes a large collection of entities in a single bulk operation. Entities must have valid primary keys.

**Example**
```csharp
var obsoleteOrders = await (await evaluator.EvaluateQueryAsync(spec)).ToListAsync();

var config = new BulkConfig { BatchSize = 1000 };
await evaluator.BulkDeleteAsync(obsoleteOrders, config);
```

---

### `BulkInsertOrUpdateAsync`

**Signature**
```csharp
Task BulkInsertOrUpdateAsync(IEnumerable<T> entities, BulkConfig? bulkConfig = null, CancellationToken cancellationToken = default)
```

**Description**
Inserts new entities and updates existing ones in a single bulk operation (bulk upsert). Existence is determined by comparing primary keys or the properties defined in `PropertiesToIncludeOnCompare`.

`BulkConfig` options of interest:
- `BatchSize`
- `SetOutputIdentity` — retrieves generated keys for new inserts
- `PropertiesToIncludeOnCompare` — defines which columns determine entity existence

**Example**
```csharp
var products = new List<Product>
{
    new Product { Id = 1, Name = "Existing Product", Price = 15.99m },
    new Product { Name = "New Product", Price = 7.99m },
};

var config = new BulkConfig
{
    BatchSize = 1000,
    SetOutputIdentity = true,
    PropertiesToIncludeOnCompare = new List<string> { nameof(Product.Id) }
};

await evaluator.BulkInsertOrUpdateAsync(products, config);
```
