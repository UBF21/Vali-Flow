# Vali-Flow.InMemory

**Synchronous in-memory evaluator for testing, caching, and offline evaluation.**

Evaluate the same reusable `ValiFlow<T>` filters against `IEnumerable<T>` without a database — perfect for unit tests, caching layers, and offline scenarios.

Part of the [Vali-Flow ecosystem](https://github.com/UBF21/vali-flow) — write filters once, use them everywhere (EF Core, SQL, in-memory, MongoDB, Elasticsearch, etc).

**Supported platforms:** .NET 8.0, .NET 9.0

---

## When to Use

✅ **Use Vali-Flow.InMemory for:**
- Unit testing repositories without mocking EF Core
- Caching layers with fresh evaluations
- Testing business logic without database I/O
- Offline evaluation against in-memory collections
- Filtering cached collections in-process

❌ **Don't use for:**
- Real-time data processing of millions of rows (use Vali-Flow + EF Core)
- Persistence beyond the application lifetime

---

## Installation

```bash
dotnet add package Vali-Flow.InMemory
```

**Dependencies:**
- Vali-Flow.Core (v2.0.0+) — expression builder
- Vali-Flow.Abstractions (v1.0.0+) — shared interfaces
- No external database dependencies ✨

---

## Quick Start

### 1. Create an evaluator

```csharp
using Vali_Flow.InMemory;

var orders = new List<Order>
{
    new Order { Id = 1, Status = "Active", Total = 150m, CreatedAt = DateTime.Now },
    new Order { Id = 2, Status = "Pending", Total = 80m, CreatedAt = DateTime.Now.AddDays(-5) },
    new Order { Id = 3, Status = "Active", Total = 220m, CreatedAt = DateTime.Now.AddDays(-10) },
};

var evaluator = new ValiFlowEvaluator<Order, int>(
    orders,
    null,  // optional: initial filter
    x => x.Id);  // key selector
```

### 2. Build a filter

```csharp
using Vali_Flow.Core;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);
```

### 3. Evaluate synchronously

```csharp
// Single result
var order = evaluator.Evaluate(filter);

// List of results
var activeHighValue = evaluator.EvaluateAll(valiFlow: filter);

// Count
var count = evaluator.EvaluateCount(filter);

// Boolean check
var hasActive = evaluator.EvaluateAny(filter);

// Paginated
var page1 = evaluator.EvaluatePaged(
    valiFlow: filter,
    pageIndex: 0,
    pageSize: 10);
```

---

## Read Operations

### Basic Queries

```csharp
// Get first matching
var first = evaluator.GetFirst(filter);

// Get last matching
var last = evaluator.GetLast(filter);

// All matching
var all = evaluator.EvaluateAll(valiFlow: filter);

// With ordering
var sorted = evaluator.EvaluateAll(
    valiFlow: filter,
    orderBy: x => x.CreatedAt);

// Paginated with metadata
var result = evaluator.EvaluatePaged(
    valiFlow: filter,
    pageIndex: 0,
    pageSize: 20);
// result.Items, result.TotalCount, result.TotalPages, result.HasNextPage
```

### Filtering & Selection

```csharp
// Distinct values
var statuses = evaluator.EvaluateDistinct(
    valiFlow: filter,
    selector: x => x.Status);

// Duplicates (entities with duplicate keys)
var duplicates = evaluator.EvaluateDuplicates(
    selector: x => x.CustomerId);

// Top N
var top10 = evaluator.EvaluateTop(
    valiFlow: filter,
    pageSize: 10,
    orderBy: x => x.Total);
```

### Aggregates

```csharp
// Sum
var totalRevenue = evaluator.EvaluateSum(
    valiFlow: filter,
    selector: x => x.Total);

// Average
var avgPrice = evaluator.EvaluateAverage(
    valiFlow: filter,
    selector: x => x.Total);

// Min / Max
var minTotal = evaluator.EvaluateMin(filter, x => x.Total);
var maxDate = evaluator.EvaluateMax(filter, x => x.CreatedAt);

// Generic aggregate
var result = evaluator.EvaluateAggregate(
    valiFlow: filter,
    selector: x => x.Total,
    aggregator: (a, b) => a + b);
```

---

## Grouped Operations

```csharp
// Group by status, count per group
var countByStatus = evaluator.EvaluateCountByGroup(
    valiFlow: filter,
    groupKeySelector: x => x.Status);
// Returns: Dictionary<string, int> { "Active" => 2, "Pending" => 1 }

// Sum by region
var revenueByRegion = evaluator.EvaluateSumByGroup(
    valiFlow: filter,
    groupKeySelector: x => x.Region,
    selector: x => x.Total);

// Min / Max by group
var maxOrderPerCustomer = evaluator.EvaluateMaxByGroup(
    valiFlow: filter,
    groupKeySelector: x => x.CustomerId,
    selector: x => x.Total);

// Top N per group
var top3PerStatus = evaluator.EvaluateTopByGroup(
    valiFlow: filter,
    groupKeySelector: x => x.Status,
    pageSize: 3,
    orderBy: x => x.Total);

// Unique & Duplicates by group
var uniqueCustomers = evaluator.EvaluateUniquesByGroup(
    groupKeySelector: x => x.CustomerId);

var duplicateOrders = evaluator.EvaluateDuplicatesByGroup(
    groupKeySelector: x => x.CustomerId);
```

---

## Write Operations

### CRUD

```csharp
// Create
var newOrder = new Order { Id = 4, Status = "Active", Total = 300m };
evaluator.Add(newOrder);
evaluator.SaveChanges();

// Read (via queries above)

// Update
var order = evaluator.Evaluate(filter);
order.Status = "Shipped";
evaluator.Update(order);

// Delete
evaluator.Delete(order);
evaluator.SaveChanges();

// Batch operations
evaluator.AddRange(new[] { order1, order2, order3 });
evaluator.DeleteRange(new[] { order1, order2 });
evaluator.SaveChanges();
```

### Upsert

```csharp
// Insert or update by key
var order = new Order { Id = 1, Status = "Archived" };
var upserted = evaluator.Upsert(order);
evaluator.SaveChanges();

// Batch upsert
var orders = new[] { order1, order2, order3 };
evaluator.UpsertRange(orders);
evaluator.SaveChanges();
```

### Delete by Condition

```csharp
// Delete all inactive orders older than 90 days
var oldInactiveFilter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Inactive")
    .IsBefore(x => x.CreatedAt, DateTime.Now.AddDays(-90));

evaluator.DeleteByCondition(oldInactiveFilter);
evaluator.SaveChanges();
```

---

## Testing Patterns

### Unit Testing Repositories

```csharp
[Fact]
public void GetActiveOrders_ReturnsOnlyActive()
{
    // Arrange
    var orders = new List<Order>
    {
        new Order { Id = 1, Status = "Active", Total = 150m },
        new Order { Id = 2, Status = "Inactive", Total = 80m },
    };

    var evaluator = new ValiFlowEvaluator<Order, int>(
        orders, null, x => x.Id);

    var filter = new ValiFlow<Order>()
        .EqualTo(x => x.Status, "Active");

    // Act
    var result = evaluator.EvaluateAll(valiFlow: filter);

    // Assert
    Assert.Single(result);
    Assert.Equal("Active", result[0].Status);
}
```

### Mock Repository with In-Memory Evaluator

```csharp
public class MockOrderRepository : IOrderRepository
{
    private readonly ValiFlowEvaluator<Order, int> _evaluator;

    public MockOrderRepository(List<Order> initialData)
    {
        _evaluator = new ValiFlowEvaluator<Order, int>(
            initialData, null, x => x.Id);
    }

    public Order GetById(int id)
    {
        var filter = new ValiFlow<Order>().EqualTo(x => x.Id, id);
        return _evaluator.Evaluate(filter);
    }

    public List<Order> GetActive()
    {
        var filter = new ValiFlow<Order>().EqualTo(x => x.Status, "Active");
        return _evaluator.EvaluateAll(valiFlow: filter);
    }

    public void Save(Order order) => _evaluator.Upsert(order);
}

// Usage in tests
[Fact]
public void OrderService_CompleteOrder_Updates()
{
    var initialOrders = new List<Order> { /* ... */ };
    var repo = new MockOrderRepository(initialOrders);
    var service = new OrderService(repo);

    service.CompleteOrder(1);

    var updated = repo.GetById(1);
    Assert.Equal("Completed", updated.Status);
}
```

---

## Caching Layer Pattern

```csharp
public class CachedOrderRepository
{
    private List<Order> _cache = new();
    private readonly ValiFlowEvaluator<Order, int> _evaluator;

    public CachedOrderRepository()
    {
        _evaluator = new ValiFlowEvaluator<Order, int>(
            _cache, null, x => x.Id);
    }

    public async void RefreshCache(IOrderRepository db)
    {
        _cache = await db.GetAllAsync();
        _evaluator.SetValiFlow(null); // Reset filter
    }

    public List<Order> Query(ValiFlow<Order> filter)
    {
        return _evaluator.EvaluateAll(valiFlow: filter);
    }

    public void InvalidateCache() => _cache.Clear();
}
```

---

## Runtime Filter Updates

```csharp
var evaluator = new ValiFlowEvaluator<Order, int>(orders, null, x => x.Id);

// Set initial filter
var filter1 = new ValiFlow<Order>().EqualTo(x => x.Status, "Active");
evaluator.SetValiFlow(filter1);

var result1 = evaluator.EvaluateAll(); // Uses filter1

// Change filter at runtime
var filter2 = new ValiFlow<Order>().GreaterThan(x => x.Total, 100m);
evaluator.SetValiFlow(filter2);

var result2 = evaluator.EvaluateAll(); // Uses filter2
```

---

## Key Differences from Vali-Flow (EF Core)

| Feature | Vali-Flow | Vali-Flow.InMemory |
|---------|-----------|-------------------|
| **Async** | Yes (`EvaluateAsync`) | No (synchronous) |
| **Target** | DbContext (EF Core) | `IEnumerable<T>` |
| **Includes** | Yes (navigation properties) | No (all data in memory) |
| **Lazy loading** | Supported | N/A (loaded upfront) |
| **Best for** | Production databases | Testing, caching, offline |
| **Performance** | Database-dependent | O(n) LINQ-to-Objects |

---

## Performance Characteristics

- **Filtering:** O(n) — LINQ-to-Objects scans entire collection
- **Ordering:** O(n log n) — LINQ sort
- **Pagination:** O(n) — must enumerate to skip/take
- **Aggregates:** O(n) — single pass (no grouping) or O(n log n) (with grouping)

For datasets > 100K rows, cache only what you need or use Vali-Flow + EF Core.

---

## Combine with Vali-Flow

Same filter works with both evaluators:

```csharp
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

// In production: EF Core
var efEvaluator = new ValiFlowEvaluator<Order>(dbContext);
var dbOrders = await efEvaluator.EvaluateQueryAsync(
    new QuerySpecification<Order>().WithFilter(filter));

// In tests: in-memory
var memEvaluator = new ValiFlowEvaluator<Order, int>(testData, filter, x => x.Id);
var testOrders = memEvaluator.EvaluateAll();

// Same business logic, different storage ✅
```

---

## Full Ecosystem

- **Vali-Flow.Core** — expression builder (`ValiFlow<T>`)
- **Vali-Flow** — EF Core async evaluator
- **Vali-Flow.InMemory** — synchronous in-memory evaluator (this package)
- **Vali-Flow.Sql** — parameterized SQL translator (Dapper/ADO.NET)
- **Vali-Flow.NoSql.\*** — MongoDB, Elasticsearch, Redis, DynamoDB adapters

See [main repository](https://github.com/UBF21/vali-flow) for complete docs.

---

## License

MIT
