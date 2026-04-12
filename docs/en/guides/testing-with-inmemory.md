# Testing with Vali-Flow.InMemory

`Vali-Flow.InMemory` is a synchronous, dependency-free evaluator that runs the same filter logic as the EF Core evaluator — but over an `IEnumerable<T>` list instead of a `DbContext`. This makes it ideal for unit tests and for caching layers that need to filter already-loaded data.

---

## Why use InMemory for tests?

- **No database or EF Core setup** — no `DbContextOptions`, no in-memory SQLite, no migrations.
- **Synchronous** — no `async/await` overhead in test assertions.
- **Same filter API** — the same `ValiFlow<T>` builders used in production are tested exactly.
- **Mutable** — add, update, remove items during a test and re-query without rebuilding the evaluator.

---

## Setup

```bash
dotnet add package Vali-Flow.InMemory
```

The constructor signature:

```csharp
new ValiFlowEvaluator<T, TProperty>(
    IEnumerable<T> initialData,
    ValiFlow<T>?   filter,
    Func<T, TProperty> getId
)
```

| Parameter | Purpose |
|-----------|---------|
| `initialData` | The seed data. The evaluator copies the list internally. |
| `filter` | Optional base filter applied to every query. Can be `null` for no default filter. |
| `getId` | Selector that identifies each entity (used by update/delete by ID). |

---

## Basic example

```csharp
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Xunit;

public class ProductQueryTests
{
    private static List<Product> Seed() => new()
    {
        new() { Id = 1, Name = "Widget",    Price = 29.99m,  IsActive = true,  Category = "Tools" },
        new() { Id = 2, Name = "Gadget",    Price = 149.99m, IsActive = true,  Category = "Electronics" },
        new() { Id = 3, Name = "Doohickey", Price = 9.99m,   IsActive = false, Category = "Tools" },
        new() { Id = 4, Name = "Thingamajig", Price = 49.99m, IsActive = true, Category = "Tools" },
    };

    [Fact]
    public void ActiveProductsAbove20_ReturnsCorrectCount()
    {
        var filter = new ValiFlow<Product>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Price, 20m);

        var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), filter, p => p.Id);

        int count = evaluator.EvaluateCount();

        Assert.Equal(3, count);
    }

    [Fact]
    public void GetFirst_ReturnsLowestIdMatch()
    {
        var filter = new ValiFlow<Product>().EqualTo(x => x.Category, "Tools");
        var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), filter, p => p.Id);

        Product? first = evaluator.EvaluateGetFirst();

        Assert.NotNull(first);
        Assert.Equal(1, first.Id);
    }
}
```

---

## Testing write operations

The evaluator exposes the same write surface as the EF Core evaluator, but all operations are synchronous and mutate the in-memory store:

```csharp
[Fact]
public void Add_NewProduct_IncreasesCount()
{
    var evaluator = new ValiFlowEvaluator<Product, int>(new List<Product>(), null, p => p.Id);

    evaluator.Add(new Product { Id = 10, Name = "New", Price = 5m, IsActive = true });

    Assert.Equal(1, evaluator.EvaluateCount());
}

[Fact]
public void Delete_RemovesMatchingItems()
{
    var data = new List<Product>
    {
        new() { Id = 1, IsActive = true,  Price = 10m },
        new() { Id = 2, IsActive = false, Price = 10m },
    };
    var evaluator = new ValiFlowEvaluator<Product, int>(data, null, p => p.Id);

    var inactive = new ValiFlow<Product>().EqualTo(x => x.IsActive, false);
    evaluator.DeleteByCondition(inactive);

    Assert.Equal(1, evaluator.EvaluateCount());
}
```

---

## Testing pagination and ordering

```csharp
[Fact]
public void EvaluatePaged_ReturnsCorrectPage()
{
    var products = Enumerable.Range(1, 20)
        .Select(i => new Product { Id = i, Name = $"P{i}", Price = i * 10m, IsActive = true })
        .ToList();

    var evaluator = new ValiFlowEvaluator<Product, int>(products, null, p => p.Id);

    // Page 2, 5 items per page
    var page = evaluator.EvaluatePaged(page: 2, pageSize: 5).ToList();

    Assert.Equal(5, page.Count);
    Assert.Equal(6, page.First().Id);
}
```

---

## Reusing a filter across unit and integration tests

A common pattern is to define your filter in a shared method and run it against both the InMemory evaluator (fast, unit test) and the real EF Core evaluator (slower, integration test):

```csharp
// Shared filter factory
public static ValiFlow<Product> ActiveAffordableFilter() =>
    new ValiFlow<Product>()
        .EqualTo(x => x.IsActive, true)
        .LessThanOrEqualTo(x => x.Price, 100m);

// Unit test — InMemory
[Fact]
public void Unit_ActiveAffordable_Count()
{
    var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), null, p => p.Id);
    int count = evaluator.EvaluateCount(ActiveAffordableFilter());
    Assert.Equal(2, count);
}

// Integration test — EF Core (requires a real or SQLite DbContext)
[Fact]
public async Task Integration_ActiveAffordable_Count()
{
    await using var context = CreateTestDbContext();
    var evaluator = new ValiFlowEvaluator<Product>(context);
    var spec = new QuerySpecification<Product>(ActiveAffordableFilter());
    int count = await evaluator.EvaluateCountAsync(spec);
    Assert.Equal(2, count);
}
```

This ensures the same filter behavior is validated at both levels without duplicating logic.

---

## Testing aggregate methods

```csharp
[Fact]
public void Sum_PriceOfActiveProducts()
{
    var filter = new ValiFlow<Product>().EqualTo(x => x.IsActive, true);
    var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), filter, p => p.Id);

    decimal total = evaluator.EvaluateSum(x => x.Price);

    Assert.Equal(229.97m, total);
}

[Fact]
public void GroupCount_ByCategory()
{
    var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), null, p => p.Id);

    var counts = evaluator.EvaluateCountByGroup(x => x.Category).ToList();

    Assert.Equal(3, counts.First(g => g.Key == "Tools").Count);
}
```

---

## Tips

- Pass `null` as the `filter` parameter when you want to start with no global filter and pass different filters per call.
- The evaluator accepts an `IEnumerable<T>` but iterates it eagerly — modifications to the source list after construction have no effect.
- For Upsert scenarios, `getId` must return a value that uniquely identifies each entity — duplicates will cause incorrect update behavior.
- Use `EvaluateAny()` as a guard in tests to assert that a state was reached before asserting more detailed properties.
