# Primeros pasos con Vali-Flow

Esta guía te lleva a través de la instalación y uso del ecosistema Vali-Flow desde cero. Construirás tu primer filtro, lo ejecutarás y entenderás qué paquete usar en cada escenario.

## Requisitos previos

- .NET 8 o .NET 9
- Un proyecto C# (cualquier tipo: Web API, consola, proyecto de tests, etc.)

---

## ¿Qué es Vali-Flow?

Vali-Flow es un conjunto de paquetes NuGet que te permiten expresar condiciones de acceso a datos como árboles de expresión C# fuertemente tipados y luego traducirlos a la tecnología de persistencia que elijas:

| Paquete | Cuándo usarlo |
|---------|--------------|
| `Vali-Flow` | Usas Entity Framework Core (BD relacional) |
| `Vali-Flow.InMemory` | Necesitas un evaluador en memoria sincrónico (tests unitarios, caché) |
| `Vali-Flow.Sql` | Usas Dapper, ADO.NET, o cualquier enfoque de SQL puro |
| `Vali-Flow.NoSql.MongoDB` | Tu almacén es MongoDB |
| `Vali-Flow.NoSql.DynamoDB` | Tu almacén es AWS DynamoDB |
| `Vali-Flow.NoSql.Elasticsearch` | Tu almacén es Elasticsearch |
| `Vali-Flow.NoSql.Redis` | Tu almacén es Redis (con RediSearch) |

Todos los paquetes comparten el mismo constructor de expresiones `ValiFlow<T>` de `Vali-Flow.Core` — el constructor es una dependencia transitiva, nunca se instala directamente.

---

## 1. EF Core — Inicio rápido

### Instalar

```bash
dotnet add package Vali-Flow
```

### Definir una entidad

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

### Construir un filtro

```csharp
using Vali_Flow.Core.Builder;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 10m)
    .LessThanOrEqualTo(x => x.Price, 500m);
```

`ValiFlow<T>` es un constructor fluido — cada método devuelve la misma instancia, por lo que encadenas condiciones con `.`. Todas las condiciones se combinan con AND lógico por defecto.

### Crear un evaluador

```csharp
using Vali_Flow.Classes.Evaluators;

// Asumiendo que tienes un DbContext de EF Core
var evaluator = new ValiFlowEvaluator<Product>(context);
```

### Consultar

```csharp
using Vali_Flow.Classes.Specifications;

var spec = new QuerySpecification<Product>(filter);

// Obtener todos los productos que coinciden
IEnumerable<Product> products = await evaluator.EvaluateQueryAsync(spec);

// Obtener conteo
int count = await evaluator.EvaluateCountAsync(spec);

// Obtener el primero o null
Product? first = await evaluator.EvaluateGetFirstAsync(spec);
```

### Agregar, actualizar, eliminar

```csharp
var newProduct = new Product { Name = "Widget", Price = 29.99m, IsActive = true };

// Agregar
await evaluator.AddAsync(newProduct);

// Actualizar (entidad ya rastreada)
newProduct.Price = 34.99m;
await evaluator.UpdateAsync(newProduct);

// Eliminar por condición — elimina todos los que coinciden sin cargarlos primero
await evaluator.DeleteByConditionAsync(filter);
```

---

## 2. In-Memory — Inicio rápido

Usa `Vali-Flow.InMemory` cuando quieras filtrar una lista en memoria con exactamente la misma API:

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

// Sincrónico — no se necesita async/await
IEnumerable<Product> result = evaluator.EvaluateQuery();
int count = evaluator.EvaluateCount();
```

---

## 3. SQL — Inicio rápido

Usa `Vali-Flow.Sql` para generar una cláusula WHERE parametrizada para Dapper o ADO.NET:

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

// Usar con Dapper
var products = await connection.QueryAsync<Product>(
    $"SELECT * FROM Products WHERE {sql.Sql}",
    sql.Parameters
);
```

Dialectos disponibles: `SqlServerDialect`, `PostgreSqlDialect`, `MySqlDialect`, `SqliteDialect`, `OracleDialect`.

---

## 4. NoSQL — Inicio rápido

Cada paquete NoSQL sigue el mismo patrón: llama a `.ToXxx()` en el constructor y pasa el resultado al driver.

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

## Operadores lógicos

Por defecto, las condiciones se unen con AND. Usa `.Or()` y `.Not()` para otras composiciones:

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

## Próximos pasos

- **Referencia del evaluador EF Core** → [`vali-flow-ef-core.md`](../vali-flow-ef-core.md)
- **Referencia In-Memory** → [`vali-flow-inmemory.md`](../vali-flow-inmemory.md)
- **Referencia del constructor SQL** → [`vali-flow-sql.md`](../vali-flow-sql.md)
- **Testing con InMemory** → [`testing-with-inmemory.md`](testing-with-inmemory.md)
- **Combinando paquetes** → [`combining-packages.md`](combining-packages.md)
- **Visión general de la arquitectura** → [`../architecture/overview.md`](../architecture/overview.md)
