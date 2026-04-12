# Combinando paquetes de Vali-Flow

Los paquetes del ecosistema Vali-Flow son independientes pero están diseñados para ser compuestos. Esta guía muestra patrones prácticos para usar múltiples paquetes juntos en una sola aplicación.

---

## La base compartida

Todos los paquetes traducen el mismo árbol de expresiones `ValiFlow<T>`. Construir un filtro una vez y reutilizarlo en diferentes tecnologías es el valor central del ecosistema:

```csharp
// Definir el filtro una vez
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m);

// Usar con EF Core
var efSpec = new QuerySpecification<Order>(filter);
var dbOrders = await efEvaluator.EvaluateQueryAsync(efSpec);

// Usar con Dapper (SQL)
var sql = filter.ToSql(new PostgreSqlDialect());
var sqlOrders = await conn.QueryAsync<Order>($"SELECT * FROM orders WHERE {sql.Sql}", sql.Parameters);

// Usar con lista en memoria
var inMemOrders = inMemEvaluator.EvaluateQuery(filter);

// Usar con MongoDB
var mongoFilter = filter.ToMongo();
var mongoOrders = await collection.Find(mongoFilter).ToListAsync();
```

Las mismas condiciones, cuatro tecnologías de persistencia, cero duplicación.

---

## Patrón 1: EF Core + InMemory (paridad test/producción)

La combinación más común. El código de producción usa el evaluador EF Core; los tests usan el evaluador InMemory con el mismo filtro.

```csharp
// Especificación compartida
public class ActiveOrdersSpec : QuerySpecification<Order>
{
    public ActiveOrdersSpec(decimal minTotal) : base(
        new ValiFlow<Order>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThanOrEqualTo(x => x.Total, minTotal))
    { }
}

// Producción
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
public void ActiveOrders_SobreUmbral_FiltraCorrectamente()
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

## Patrón 2: EF Core + SQL (separación lectura/escritura)

Usa EF Core para escrituras y consultas relacionales complejas; usa `Vali-Flow.Sql` + Dapper para consultas de lectura intensiva con alta performance.

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

    // Camino de escritura — EF Core (change tracking, validación, etc.)
    public Task<Order> CreateOrderAsync(Order order)
        => _ef.AddAsync(order);

    // Camino de lectura — Dapper (SQL puro, JOINs, proyecciones)
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

## Patrón 3: Fan-out multi-almacén

Algunas aplicaciones escriben a múltiples almacenes (p.ej., una BD relacional como fuente de verdad y Elasticsearch para búsquedas). Un único filtro puede usarse para leer de ambos:

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

        // Camino de búsqueda — Elasticsearch (full-text, rápido)
        var esQuery = filter.ToElasticsearch();
        var esResult = await _es.SearchAsync<Product>(s => s.Query(esQuery).Size(20));

        // Fallback / validación — EF Core
        var spec = new QuerySpecification<Product>(filter);
        int dbCount = await _ef.EvaluateCountAsync(spec);

        return new SearchResult
        {
            Items   = esResult.Documents,
            DbCount = dbCount,
        };
    }
}
```

---

## Patrón 4: Capa de caché con InMemory

Cargar un dataset en memoria una vez, luego servir consultas de filtro desde la caché sin acceder a la base de datos:

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

## Patrón 5: Negación transversal

`negateCondition: true` está disponible en los métodos de lectura e invierte el filtro. Úsalo para consultar ambos lados de una condición sin construir dos filtros separados:

```csharp
var filter = new ValiFlow<Product>().EqualTo(x => x.IsActive, true);
var spec = new QuerySpecification<Product>(filter);

// Productos activos
var active = await evaluator.EvaluateQueryAsync(spec);

// Productos inactivos — mismo filtro, negado
var inactive = await evaluator.EvaluateQueryAsync(spec, negateCondition: true);
```

---

## Configuración de inyección de dependencias (EF Core)

```csharp
// Program.cs / Startup.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped(typeof(ValiFlowEvaluator<>), typeof(ValiFlowEvaluator<>));

// Uso en un servicio
public class OrderService
{
    private readonly ValiFlowEvaluator<Order> _evaluator;

    public OrderService(ValiFlowEvaluator<Order> evaluator)
        => _evaluator = evaluator;
}
```

---

## Ver también

- [Primeros pasos](getting-started.md)
- [Testing con InMemory](testing-with-inmemory.md)
- [Referencia EF Core](../vali-flow-ef-core.md)
- [Referencia SQL](../vali-flow-sql.md)
