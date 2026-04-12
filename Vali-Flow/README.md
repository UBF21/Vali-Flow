# Vali-Flow

EF Core async evaluator for .NET — built on top of `Vali-Flow.Core`. Exposes a fluent specification API to encapsulate filter, ordering, pagination, and eager-loading logic in reusable, composable objects.

## Install

```bash
dotnet add package Vali-Flow
```

## Quick Start

```csharp
var evaluator = new ValiFlowEvaluator<Order>(dbContext);

var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .EqualTo(x => x.Status, "Active")
        .GreaterThan(x => x.Total, 0m))
    .WithAsNoTracking(true);

bool  hasOrders = await evaluator.EvaluateAnyAsync(spec, cancellationToken);
int   count     = await evaluator.EvaluateCountAsync(spec, cancellationToken);
Order? first    = await evaluator.EvaluateGetFirstAsync(spec, cancellationToken);
```

## Specifications

### BasicSpecification\<T\>

Filter, eager loading, and EF Core query hints — no ordering or pagination.

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

### QuerySpecification\<T\>

Extends `BasicSpecification<T>` with ordering, pagination, and top-N.

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

## Reading Data

### Existence and count

```csharp
bool exists = await evaluator.EvaluateAnyAsync(spec, cancellationToken);
int  count  = await evaluator.EvaluateCountAsync(spec, cancellationToken);
```

### Single entity

```csharp
Order? first = await evaluator.EvaluateGetFirstAsync(spec, cancellationToken);
Order? last  = await evaluator.EvaluateGetLastAsync(spec, cancellationToken);

// Entities that do NOT match the specification
Order? firstFailed = await evaluator.EvaluateGetFirstFailedAsync(spec, cancellationToken);
Order? lastFailed  = await evaluator.EvaluateGetLastFailedAsync(spec, cancellationToken);
```

### Queryable projection

```csharp
IQueryable<Order> query  = await evaluator.EvaluateQueryAsync(querySpec);
var totals = await query.Select(o => o.Total).ToListAsync(cancellationToken);
```

### Distinct and duplicates

```csharp
IQueryable<Order> distinct = await evaluator.EvaluateDistinctAsync(
    querySpec, selector: x => x.CustomerId, cancellationToken);

IQueryable<Order> duplicates = await evaluator.EvaluateDuplicatesAsync(
    querySpec, selector: x => x.CustomerId, cancellationToken);
```

### Aggregates

```csharp
decimal min = await evaluator.EvaluateMinAsync(spec, x => x.Total, cancellationToken);
decimal max = await evaluator.EvaluateMaxAsync(spec, x => x.Total, cancellationToken);
decimal avg = await evaluator.EvaluateAverageAsync(spec, x => x.Total, cancellationToken);
decimal sum = await evaluator.EvaluateSumAsync(spec, x => x.Total, cancellationToken);
```

### Grouped aggregates

```csharp
Dictionary<string, int>     countByStatus =
    await evaluator.EvaluateCountByGroupAsync(spec, x => x.Status, cancellationToken);

Dictionary<string, decimal> sumByStatus =
    await evaluator.EvaluateSumByGroupAsync(spec, x => x.Status, x => x.Total, cancellationToken);

Dictionary<string, decimal> avgByStatus =
    await evaluator.EvaluateAverageByGroupAsync(spec, x => x.Status, x => x.Total, cancellationToken);

Dictionary<string, List<Order>> grouped =
    await evaluator.EvaluateGroupedAsync(spec, x => x.Status, cancellationToken);
```

### Validate a single entity

```csharp
var filter = new ValiFlow<Order>().GreaterThan(x => x.Total, 100m);
bool passes = await evaluator.EvaluateAsync(filter, order);
```

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
// Single entity — insert if no match, update otherwise
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
    await evaluator.UpdateAsync(existing, saveChanges: false, cancellationToken);
    await evaluator.SaveChangesAsync(cancellationToken);
}, cancellationToken);
```

## Bulk Operations

Uses `EFCore.BulkExtensions` for high-throughput scenarios.

```csharp
var bulkConfig = new BulkConfig { BatchSize = 1000, SetOutputIdentity = true };

await evaluator.BulkInsertAsync(orders, bulkConfig, cancellationToken);
await evaluator.BulkUpdateAsync(orders, bulkConfig, cancellationToken);
await evaluator.BulkDeleteAsync(orders, bulkConfig, cancellationToken);
await evaluator.BulkInsertOrUpdateAsync(orders, bulkConfig, cancellationToken);
```

## EF Core Query Options

| Method | Default | Effect |
|---|---|---|
| `WithAsNoTracking(bool)` | `true` | Disables change tracking for read-only queries |
| `WithAsSplitQuery(bool)` | `false` | Splits JOINs into multiple round-trips |
| `WithIgnoreQueryFilters(bool)` | `false` | Bypasses global query filters (soft-delete, multi-tenant) |

```csharp
// Retrieve soft-deleted records
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().EqualTo(x => x.CustomerId, customerId))
    .WithIgnoreQueryFilters(true)
    .WithAsNoTracking(true);
```

## Negating Conditions

All read methods accept a `negateCondition` parameter to invert the filter.

```csharp
// All orders that do NOT have Status == "Active"
IQueryable<Order> inactive = await evaluator.EvaluateQueryAsync(
    querySpec, negateCondition: true);
```

## Full documentation

[vali-flow-docs.netlify.app/docs/adapters/ef-core](https://vali-flow-docs.netlify.app/docs/adapters/ef-core)

## Contributing

Contributions, issues, and feature requests are welcome. Feel free to open a pull request or an issue on [GitHub](https://github.com/UBF21/vali-flow).

If this package is useful to you, consider supporting its development:

- **Latin America** — [MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **International** — [PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)

## License

Licensed under the [MIT License](LICENSE).  
Copyright &copy; 2025 Felipe Rafael Montenegro Morriberon. All rights reserved.
