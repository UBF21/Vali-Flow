# Vali-Flow.InMemory

## Overview

**Vali-Flow.InMemory** is a synchronous, zero-dependency in-memory evaluator for the Vali-Flow ecosystem. It operates on plain `IEnumerable<T>` collections instead of a `DbContext`, making it ideal for:

- **Unit tests** — inject a pre-populated evaluator in place of the real EF Core evaluator, no database required.
- **Caching layers** — evaluate business rules against an in-memory cache without hitting the database.
- **Prototyping** — explore Vali-Flow's API without any infrastructure setup.

### How it differs from the EF Core evaluator

| Aspect | `Vali-Flow` (EF Core) | `Vali-Flow.InMemory` |
|--------|----------------------|----------------------|
| Data source | `DbContext` / SQL database | `IEnumerable<T>` |
| Execution | Asynchronous (`async/await`) | **Synchronous** |
| Query translation | LINQ-to-SQL via EF Core | LINQ-to-Objects |
| Includes / navigation | `IEfInclude<T>` (JOINs) | Not applicable |
| Change tracking | EF Core change tracker | Internal tracked lists |

### Installation

```bash
dotnet add package Vali-Flow.InMemory
```

---

## Setup

### Constructor

```csharp
public ValiFlowEvaluator<T, TProperty>(
    IEnumerable<T>? initialData = null,
    ValiFlow<T>? valiFlow = null,
    Func<T, TProperty>? getId = null)
```

**Type parameters**

| Parameter | Description |
|-----------|-------------|
| `T` | Entity type (must be a reference type). |
| `TProperty` | Type of the entity's primary key (e.g., `int`, `Guid`, `string`). |

**Constructor parameters**

| Parameter | Type | Description |
|-----------|------|-------------|
| `initialData` | `IEnumerable<T>?` | Seed data copied into the internal store. Pass `null` for an empty store. |
| `valiFlow` | `ValiFlow<T>?` | Default filter applied by every method when no per-call filter is supplied. Pass `null` to match all entities. |
| `getId` | `Func<T, TProperty>?` | Key extractor used by write operations to identify entities. If `null`, the constructor looks for a public property named `Id` via reflection (cached once at construction time). Throws `InvalidOperationException` if `Id` is not found. |

**Example**

```csharp
var products = new List<Product>
{
    new() { Id = 1, Name = "Widget", Price = 10m, IsActive = true },
    new() { Id = 2, Name = "Gadget", Price = 120m, IsActive = true },
    new() { Id = 3, Name = "Doohickey", Price = 5m, IsActive = false },
};

var filter = new ValiFlow<Product>().IsTrue(p => p.IsActive);

// Explicit key extractor
var evaluator = new ValiFlowEvaluator<Product, int>(
    initialData: products,
    valiFlow: filter,
    getId: p => p.Id);

// Auto-detect 'Id' property (Product has a public int Id)
var evaluatorAuto = new ValiFlowEvaluator<Product, int>(products, filter);
```

---

## The `negateCondition` Parameter

Every read method accepts a `bool negateCondition` parameter (default `false`). When set to `true`, the ValiFlow filter is logically inverted using `BuildNegated()` internally — equivalent to wrapping the entire expression in a `!`.

This is useful when you want to query items that **fail** a validation rule without defining a separate filter.

### Before/after example

```csharp
var filter = new ValiFlow<Product>().GreaterThan(p => p.Price, 50m);

var evaluator = new ValiFlowEvaluator<Product, int>(products, filter, p => p.Id);

// Normal: products with Price > 50
IEnumerable<Product> expensive = evaluator.EvaluateAll<int>(null);

// Negated: products with Price <= 50  (same filter, inverted)
IEnumerable<Product> cheap = evaluator.EvaluateAll<int>(null, negateCondition: true);
```

> **Note:** Negated conditions derived from the instance-level `_valiFlow` are compiled once and cached. Passing an explicit `valiFlow` per-call bypasses the cache and compiles on every call.

### Behaviour table

| Method family | `negateCondition = false` | `negateCondition = true` |
|---------------|--------------------------|--------------------------|
| `EvaluateAll` | Entities matching the filter | Entities **not** matching the filter |
| `EvaluateAllFailed` | Entities **not** matching the filter | Entities matching the filter |
| `GetFirst` / `GetLast` | First/last matching entity | First/last **non-matching** entity |
| `GetFirstFailed` / `GetLastFailed` | First/last **non-matching** entity | First/last matching entity |
| Aggregates / grouping | Computed over matching entities | Computed over **non-matching** entities |

---

## Read Methods

### `Evaluate`

**Signature**
```csharp
bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Description**
Evaluates a single entity against the filter. Returns `true` if the entity satisfies the condition; `false` otherwise.

**Example**
```csharp
var product = new Product { Id = 1, Price = 80m, IsActive = true };
var filter = new ValiFlow<Product>().GreaterThan(p => p.Price, 50m);
var evaluator = new ValiFlowEvaluator<Product, int>(getId: p => p.Id);

bool passes = evaluator.Evaluate(product, filter); // true
bool fails  = evaluator.Evaluate(product, filter, negateCondition: true); // false
```

---

### `EvaluateAny`

**Signature**
```csharp
bool EvaluateAny(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Description**
Returns `true` if at least one entity in the collection satisfies the filter. Pass `null` for `entities` to operate on the internal store.

**Example**
```csharp
bool hasExpensive = evaluator.EvaluateAny(null);
// true if any product in the store has Price > 50
```

---

### `EvaluateCount`

**Signature**
```csharp
int EvaluateCount(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Description**
Counts the number of entities that satisfy the filter.

**Example**
```csharp
int activeCount = evaluator.EvaluateCount(null);
// returns 2 (Widget and Gadget are active)

int inactiveCount = evaluator.EvaluateCount(null, negateCondition: true);
// returns 1 (Doohickey is inactive)
```

---

### `GetFirst`

**Signature**
```csharp
T? GetFirst(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Description**
Returns the first entity that satisfies the filter, or `null` if none match. No ordering is applied; the result depends on enumeration order.

**Example**
```csharp
Product? first = evaluator.GetFirst(null);
// Returns the first active product in store order
```

---

### `GetFirstFailed`

**Signature**
```csharp
T? GetFirstFailed(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Description**
Returns the first entity that **does not** satisfy the filter. When `negateCondition = true`, the logic inverts and returns the first entity that satisfies the filter (equivalent to `GetFirst`).

**Example**
```csharp
Product? firstInactive = evaluator.GetFirstFailed(null);
// Returns Doohickey (IsActive = false)
```

---

### `EvaluateAll`

**Signature**
```csharp
IEnumerable<T> EvaluateAll<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns all entities that satisfy the filter, with optional primary and secondary ordering.

**Example**
```csharp
// All active products sorted by price descending
IEnumerable<Product> sorted = evaluator.EvaluateAll<decimal>(
    entities: null,
    orderBy: p => p.Price,
    ascending: false);
```

---

### `EvaluateAllFailed`

**Signature**
```csharp
IEnumerable<T> EvaluateAllFailed<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns all entities that **do not** satisfy the filter, with optional ordering. Useful for validation reports — collect everything that failed a rule.

**Example**
```csharp
IEnumerable<Product> inactive = evaluator.EvaluateAllFailed<string>(
    entities: null,
    orderBy: p => p.Name);
```

---

### `GetLast`

**Signature**
```csharp
T? GetLast<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns the last entity that satisfies the filter after optional ordering. Returns `null` if none match.

**Example**
```csharp
Product? mostExpensive = evaluator.GetLast<decimal>(
    entities: null,
    orderBy: p => p.Price,
    ascending: true);
```

---

### `GetLastFailed`

**Signature**
```csharp
T? GetLastFailed<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns the last entity that does **not** satisfy the filter after optional ordering.

**Example**
```csharp
Product? lastInactive = evaluator.GetLastFailed<string>(
    entities: null,
    orderBy: p => p.Name);
```

---

### `EvaluatePaged`

**Signature**
```csharp
IEnumerable<T> EvaluatePaged<TKey>(
    IEnumerable<T> entities,
    int page,
    int pageSize,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns a paginated slice of the filtered entities. `page` is 1-based.

> **Note:** Both `page` and `pageSize` must be >= 1, otherwise an `ArgumentOutOfRangeException` is thrown.

**Example**
```csharp
// Page 2, 10 items per page, active products sorted by name
IEnumerable<Product> page2 = evaluator.EvaluatePaged<string>(
    entities: allProducts,
    page: 2,
    pageSize: 10,
    orderBy: p => p.Name);
```

---

### `EvaluatePagedResult`

**Signature**
```csharp
PagedResult<T> EvaluatePagedResult<TKey>(
    IEnumerable<T>? entities,
    int page,
    int pageSize,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Like `EvaluatePaged`, but returns a `PagedResult<T>` that includes pagination metadata.

**`PagedResult<T>` properties**

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<T>` | Items on the current page. |
| `TotalCount` | `int` | Total matching entities across all pages. |
| `Page` | `int` | Current page number (1-based). |
| `PageSize` | `int` | Maximum items per page. |
| `TotalPages` | `int` | Total number of pages. |
| `HasNextPage` | `bool` | Whether a next page exists. |
| `HasPreviousPage` | `bool` | Whether a previous page exists. |

**Example**
```csharp
PagedResult<Product> result = evaluator.EvaluatePagedResult<decimal>(
    entities: null,
    page: 1,
    pageSize: 5,
    orderBy: p => p.Price,
    ascending: false);

Console.WriteLine($"Page {result.Page} of {result.TotalPages} — {result.TotalCount} total");
```

---

### `EvaluateTop`

**Signature**
```csharp
IEnumerable<T> EvaluateTop<TKey>(
    IEnumerable<T> entities,
    int count,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns the top N entities that satisfy the filter. `count` must be > 0.

**Example**
```csharp
// Top 3 most expensive active products
IEnumerable<Product> top3 = evaluator.EvaluateTop<decimal>(
    entities: null,
    count: 3,
    orderBy: p => p.Price,
    ascending: false);
```

---

### `EvaluateDistinct`

**Signature**
```csharp
IEnumerable<T> EvaluateDistinct<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> selector,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns one representative entity per distinct key value, keeping only the first occurrence within each group after the filter is applied.

**Example**
```csharp
// One product per category
IEnumerable<Product> onePerCategory = evaluator.EvaluateDistinct<string>(
    entities: null,
    selector: p => p.Category);
```

---

### `EvaluateDuplicates`

**Signature**
```csharp
IEnumerable<T> EvaluateDuplicates<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> selector,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns all entities whose key value appears more than once in the filtered set.

**Example**
```csharp
// Products with a duplicate SKU among active ones
IEnumerable<Product> dupSkus = evaluator.EvaluateDuplicates<string>(
    entities: null,
    selector: p => p.Sku);
```

---

### `GetFirstMatchIndex`

**Signature**
```csharp
int GetFirstMatchIndex<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns the zero-based index of the first matching entity after ordering. Returns `-1` if no entity matches.

**Example**
```csharp
int idx = evaluator.GetFirstMatchIndex<decimal>(
    entities: null,
    orderBy: p => p.Price);
// Index of the cheapest active product in the ordered list
```

---

### `GetLastMatchIndex`

**Signature**
```csharp
int GetLastMatchIndex<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Description**
Returns the zero-based index of the last matching entity after ordering. Returns `-1` if no entity matches.

**Example**
```csharp
int idx = evaluator.GetLastMatchIndex<decimal>(
    entities: null,
    orderBy: p => p.Price);
// Index of the most expensive active product in the ordered list
```

---

### `EvaluateMin`

**Signature**
```csharp
TResult EvaluateMin<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Description**
Computes the minimum value of a numeric property among filtered entities. Returns `TResult.Zero` if the filtered set is empty.

**Example**
```csharp
decimal minPrice = evaluator.EvaluateMin<decimal>(null, p => p.Price);
```

---

### `EvaluateMax`

**Signature**
```csharp
TResult EvaluateMax<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Description**
Computes the maximum value of a numeric property among filtered entities. Returns `TResult.Zero` if the filtered set is empty.

**Example**
```csharp
decimal maxPrice = evaluator.EvaluateMax<decimal>(null, p => p.Price);
```

---

### `EvaluateAverage`

**Signature**
```csharp
decimal EvaluateAverage<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Description**
Computes the average of a numeric property among filtered entities. Returns `0` if the filtered set is empty. Always returns `decimal`.

**Example**
```csharp
decimal avgPrice = evaluator.EvaluateAverage<decimal>(null, p => p.Price);
```

---

### `EvaluateSum`

**Signature**
```csharp
TResult EvaluateSum<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Description**
Computes the sum of a numeric property among filtered entities.

**Example**
```csharp
decimal totalRevenue = evaluator.EvaluateSum<decimal>(null, p => p.Price);
```

---

### `EvaluateAggregate`

**Signature**
```csharp
TResult EvaluateAggregate<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    Func<TResult, TResult, TResult> aggregator,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Description**
Applies a custom accumulator function over the selected numeric property of filtered entities. Starting accumulator value is `TResult.Zero`.

**Example**
```csharp
// Custom product of stock quantities
int product = evaluator.EvaluateAggregate<int>(
    entities: null,
    selector: p => p.Stock,
    aggregator: (acc, val) => acc + val * 2);
```

---

### `EvaluateGrouped`

**Signature**
```csharp
Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns a dictionary of key → entity list.

**Example**
```csharp
Dictionary<string, List<Product>> byCategory =
    evaluator.EvaluateGrouped<string>(null, p => p.Category);
```

---

### `EvaluateCountByGroup`

**Signature**
```csharp
Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns a dictionary of key → count.

**Example**
```csharp
Dictionary<string, int> countPerCategory =
    evaluator.EvaluateCountByGroup<string>(null, p => p.Category);
```

---

### `EvaluateSumByGroup`

**Signature**
```csharp
Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult> where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns the sum of a numeric property per group.

**Example**
```csharp
Dictionary<string, decimal> revenueByCategory =
    evaluator.EvaluateSumByGroup<string, decimal>(null, p => p.Category, p => p.Price);
```

---

### `EvaluateMinByGroup`

**Signature**
```csharp
Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult> where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns the minimum value of a numeric property per group.

**Example**
```csharp
Dictionary<string, decimal> minPriceByCategory =
    evaluator.EvaluateMinByGroup<string, decimal>(null, p => p.Category, p => p.Price);
```

---

### `EvaluateMaxByGroup`

**Signature**
```csharp
Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult> where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns the maximum value of a numeric property per group.

**Example**
```csharp
Dictionary<string, decimal> maxPriceByCategory =
    evaluator.EvaluateMaxByGroup<string, decimal>(null, p => p.Category, p => p.Price);
```

---

### `EvaluateAverageByGroup`

**Signature**
```csharp
Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult> where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns the average of a numeric property per group as `decimal`.

**Example**
```csharp
Dictionary<string, decimal> avgPriceByCategory =
    evaluator.EvaluateAverageByGroup<string, decimal>(null, p => p.Category, p => p.Price);
```

---

### `EvaluateDuplicatesByGroup`

**Signature**
```csharp
Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns only the groups that contain more than one entity (duplicate groups).

**Example**
```csharp
// Find categories with more than one active product
Dictionary<string, List<Product>> duplicates =
    evaluator.EvaluateDuplicatesByGroup<string>(null, p => p.Category);
```

---

### `EvaluateUniquesByGroup`

**Signature**
```csharp
Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns only the groups with exactly one entity (unique groups). The dictionary value is the single entity, not a list.

**Example**
```csharp
// Categories with exactly one active product
Dictionary<string, Product> uniques =
    evaluator.EvaluateUniquesByGroup<string>(null, p => p.Category);
```

---

### `EvaluateTopByGroup`

**Signature**
```csharp
Dictionary<TKey, List<T>> EvaluateTopByGroup<TKey, TOrderKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    int count,
    Func<T, TOrderKey>? orderBy = null,
    bool ascending = true,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Description**
Groups filtered entities by a key and returns the top N entities per group, with optional intra-group ordering.

**Example**
```csharp
// Top 2 most expensive products per category
Dictionary<string, List<Product>> top2PerCategory =
    evaluator.EvaluateTopByGroup<string, decimal>(
        entities: null,
        keySelector: p => p.Category,
        count: 2,
        orderBy: p => p.Price,
        ascending: false);
```

---

## Write Methods

All write methods operate on the internal store by default. Pass an explicit `entities` collection to target an external list instead.

### `Add`

**Signature**
```csharp
bool Add(T entity, IEnumerable<T>? entities = null)
```

**Description**
Adds a single entity to the store. Returns `true` on success.

**Example**
```csharp
var newProduct = new Product { Id = 4, Name = "Thingamajig", Price = 30m, IsActive = true };
bool added = evaluator.Add(newProduct);
```

---

### `AddRange`

**Signature**
```csharp
void AddRange(IEnumerable<T> entitiesToAdd, IEnumerable<T>? entities = null)
```

**Description**
Adds multiple entities to the store in one call.

**Example**
```csharp
evaluator.AddRange(new[]
{
    new Product { Id = 5, Name = "Alpha", Price = 15m, IsActive = true },
    new Product { Id = 6, Name = "Beta",  Price = 25m, IsActive = true },
});
```

---

### `Update`

**Signature**
```csharp
T? Update(T entity, IEnumerable<T>? entities = null)
```

**Description**
Replaces the stored entity with the same key as the provided entity. Returns the updated entity, or `null` if no matching entity was found.

**Example**
```csharp
var modified = new Product { Id = 1, Name = "Widget Pro", Price = 15m, IsActive = true };
Product? result = evaluator.Update(modified);
```

---

### `UpdateRange`

**Signature**
```csharp
IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, IEnumerable<T>? entities = null)
```

**Description**
Updates multiple entities. Returns the collection of successfully updated entities.

**Example**
```csharp
IEnumerable<Product> updated = evaluator.UpdateRange(modifiedProducts);
```

---

### `Delete`

**Signature**
```csharp
bool Delete(T entity, IEnumerable<T>? entities = null)
```

**Description**
Removes a single entity from the store by matching its key. Returns `true` if found and removed.

**Example**
```csharp
bool removed = evaluator.Delete(product);
```

---

### `DeleteRange`

**Signature**
```csharp
int DeleteRange(IEnumerable<T> entitiesToDelete, IEnumerable<T>? entities = null)
```

**Description**
Removes multiple entities from the store. Returns the count of successfully removed entities.

**Example**
```csharp
int count = evaluator.DeleteRange(staleProducts);
```

---

### `DeleteByCondition`

**Signature**
```csharp
int DeleteByCondition(Func<T, bool> predicate, IEnumerable<T>? entities = null)
```

**Description**
Removes all entities that satisfy a predicate. Returns the count of removed entities.

**Example**
```csharp
// Remove all inactive products
int removed = evaluator.DeleteByCondition(p => !p.IsActive);
```

---

### `Upsert`

**Signature**
```csharp
T Upsert(T entity, IEnumerable<T>? entities = null)
```

**Description**
Inserts the entity if its key does not exist in the store; updates it if the key already exists. Returns the entity after the operation.

**Example**
```csharp
var order = new Order { Id = 99, CustomerId = 1, Total = 250m };
Order upserted = evaluator.Upsert(order);
```

---

### `UpsertRange`

**Signature**
```csharp
IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, IEnumerable<T>? entities = null)
```

**Description**
Performs `Upsert` on each entity in the collection. Returns all upserted entities.

**Example**
```csharp
IEnumerable<Order> results = evaluator.UpsertRange(incomingOrders);
```

---

### `SaveChanges`

**Signature**
```csharp
void SaveChanges(IEnumerable<T>? entities = null)
```

**Description**
Applies any pending tracked changes (additions, updates, deletions) to the store. Call this after batching write operations if you deferred them.

**Example**
```csharp
evaluator.Add(productA);
evaluator.Add(productB);
evaluator.SaveChanges();
```

---

## `SetValiFlow`

**Signature**
```csharp
void SetValiFlow(ValiFlow<T> valiFlow)
```

**Description**
Replaces the instance-level filter at runtime. Clears the cached negated condition so it is recompiled on the next negated call. Throws `ArgumentNullException` if `valiFlow` is `null`.

Useful when the evaluator is shared across test cases and each case requires a different filter.

**Example**
```csharp
var evaluator = new ValiFlowEvaluator<User, int>(users, getId: u => u.Id);

// Initially no filter — all users
int allCount = evaluator.EvaluateCount(null);

// Switch to only premium users
evaluator.SetValiFlow(new ValiFlow<User>().IsTrue(u => u.IsPremium));
int premiumCount = evaluator.EvaluateCount(null);
```
