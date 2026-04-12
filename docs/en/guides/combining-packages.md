# Combining Vali-Flow Packages

The packages in the Vali-Flow ecosystem are independent but designed to be composed. This guide shows practical patterns for using multiple packages together in a single application.

---

## The shared foundation

All packages translate the same `ValiFlow<T>` expression tree. Building a filter once and reusing it across technologies is the core value proposition:

```csharp
// Define the filter once
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m);

// Use with EF Core
var efSpec = new QuerySpecification<Order>(filter);
var dbOrders = await efEvaluator.EvaluateQueryAsync(efSpec);

// Use with Dapper (SQL)
var sql = filter.ToSql(new PostgreSqlDialect());
var sqlOrders = await conn.QueryAsync<Order>($"SELECT * FROM orders WHERE {sql.Sql}", sql.Parameters);

// Use with in-memory list
var inMemOrders = inMemEvaluator.EvaluateQuery(filter);

// Use with MongoDB
var mongoFilter = filter.ToMongo();
var mongoOrders = await collection.Find(mongoFilter).ToListAsync();
```

The same conditions, four persistence technologies, zero duplication.

---

## Pattern 1: EF Core + InMemory (test/prod parity)

The most common combination. Production code uses the EF Core evaluator; tests use the InMemory evaluator with the same filter.

```csharp
// Shared specification
public class ActiveOrdersSpec : QuerySpecification<Order>
{
    public ActiveOrdersSpec(decimal minTotal) : base(
        new ValiFlow<Order>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThanOrEqualTo(x => x.Total, minTotal))
    { }
}

// Production
public class OrderService
{
    private readonly ValiFlowEvaluator<Order> _evaluator;

    public OrderService(AppDbContext context)
        => _evaluator = new ValiFlowEvaluator<Order>(context);

    public Task<IEnumerable<Order>> GetActiveOrdersAsync(decimal minTotal)
        => _evaluator.EvaluateQueryAsync(new ActiveOrdersSpec(minTotal));
}

// Tests
[Fact]
public void ActiveOrders_AboveThreshold_FiltersCorrectly()
{
    var orders = new List<Order>
    {
        new() { Id = 1, IsActive = true,  Total = 150m },
        new() { Id = 2, IsActive = false, Total = 300m },
        new() { Id = 3, IsActive = true,  Total = 50m  },
    };

    var evaluator = new ValiFlowEvaluator<Order, int>(orders, null, o => o.Id);
    var spec = new ActiveOrdersSpec(100m);

    var result = evaluator.EvaluateQuery(spec.Filter).ToList();

    Assert.Single(result);
    Assert.Equal(1, result[0].Id);
}
```

---

## Pattern 2: EF Core + SQL (read/write split)

Use EF Core for writes and complex relational queries; use `Vali-Flow.Sql` + Dapper for read-heavy, performance-sensitive queries.

```csharp
public class ReportingService
{
    private readonly ValiFlowEvaluator<Order> _ef;
    private readonly IDbConnection _conn;

    public ReportingService(AppDbContext context, IDbConnection conn)
    {
        _ef   = new ValiFlowEvaluator<Order>(context);
        _conn = conn;
    }

    // Write path — EF Core (change tracking, validation, etc.)
    public Task<Order> CreateOrderAsync(Order order)
        => _ef.AddAsync(order);

    // Read path — Dapper (raw SQL, joins, projections)
    public async Task<IEnumerable<OrderSummary>> GetSummaryAsync(decimal minTotal)
    {
        var filter = new ValiFlow<Order>()
            .GreaterThanOrEqualTo(x => x.Total, minTotal)
            .EqualTo(x => x.IsActive, true);

        var sql = filter.ToSql(new PostgreSqlDialect());

        return await _conn.QueryAsync<OrderSummary>(
            $@"SELECT o.Id, o.Total, c.Name AS CustomerName
               FROM orders o
               JOIN customers c ON c.Id = o.CustomerId
               WHERE {sql.Sql}
               ORDER BY o.Total DESC",
            sql.Parameters
        );
    }
}
```

---

## Pattern 3: Multi-store fan-out

Some applications write to multiple stores (e.g., a relational DB as source of truth and Elasticsearch for search). A single filter can be used to read from both:

```csharp
public class ProductSearchService
{
    private readonly ValiFlowEvaluator<Product> _ef;
    private readonly ElasticsearchClient _es;
    private readonly IMongoCollection<Product> _mongo;

    public async Task<SearchResult> SearchAsync(string category, decimal maxPrice)
    {
        var filter = new ValiFlow<Product>()
            .EqualTo(x => x.Category, category)
            .LessThanOrEqualTo(x => x.Price, maxPrice)
            .EqualTo(x => x.IsActive, true);

        // Search path — Elasticsearch (full-text, fast)
        var esQuery = filter.ToElasticsearch();
        var esResult = await _es.SearchAsync<Product>(s => s.Query(esQuery).Size(20));

        // Fallback / validation — EF Core
        var spec = new QuerySpecification<Product>(filter);
        int dbCount = await _ef.EvaluateCountAsync(spec);

        return new SearchResult
        {
            Items    = esResult.Documents,
            DbCount  = dbCount,
        };
    }
}
```

---

## Pattern 4: Cache layer with InMemory

Load a dataset into memory once, then serve filter queries from the cache without hitting the database:

```csharp
public class ProductCacheService
{
    private ValiFlowEvaluator<Product, int>? _cache;
    private DateTime _loadedAt;

    private async Task EnsureLoadedAsync(AppDbContext context)
    {
        if (_cache != null && DateTime.UtcNow - _loadedAt < TimeSpan.FromMinutes(5))
            return;

        var efEvaluator = new ValiFlowEvaluator<Product>(context);
        var all = await efEvaluator.EvaluateQueryAsync(new QuerySpecification<Product>(null));

        _cache    = new ValiFlowEvaluator<Product, int>(all, null, p => p.Id);
        _loadedAt = DateTime.UtcNow;
    }

    public async Task<IEnumerable<Product>> QueryAsync(ValiFlow<Product> filter, AppDbContext context)
    {
        await EnsureLoadedAsync(context);
        return _cache!.EvaluateQuery(filter);
    }
}
```

---

## Pattern 5: Cross-cutting negation

`negateCondition: true` is available on read methods and inverts the filter. Use it to query both sides of a condition without building two separate filters:

```csharp
var filter = new ValiFlow<Product>().EqualTo(x => x.IsActive, true);
var spec = new QuerySpecification<Product>(filter);

// Active products
var active = await evaluator.EvaluateQueryAsync(spec);

// Inactive products — same filter, negated
var inactive = await evaluator.EvaluateQueryAsync(spec, negateCondition: true);
```

---

## Dependency injection setup (EF Core)

```csharp
// Program.cs / Startup.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped(typeof(ValiFlowEvaluator<>), typeof(ValiFlowEvaluator<>));

// Usage in a service
public class OrderService
{
    private readonly ValiFlowEvaluator<Order> _evaluator;

    public OrderService(ValiFlowEvaluator<Order> evaluator)
        => _evaluator = evaluator;
}
```

---

## See also

- [Getting Started](getting-started.md)
- [Testing with InMemory](testing-with-inmemory.md)
- [EF Core reference](../vali-flow-ef-core.md)
- [SQL reference](../vali-flow-sql.md)
