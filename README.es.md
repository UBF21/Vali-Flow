# Vali-Flow

[English](README.md) | **Español**

## Descripción general

Vali-Flow es un ecosistema completo de librerías .NET para construir criterios de consulta reutilizables y componibles mediante una API fluida. Permite definir la lógica de filtrado una sola vez usando un DSL simple basado en expresiones, y traducirla a:

- Consultas de **Entity Framework Core** (async)
- **SQL parametrizado** para Dapper / ADO.NET (SQL Server, PostgreSQL, MySQL, SQLite)
- Filtros **MongoDB BSON**
- **Elasticsearch Query DSL**
- Consultas **Redis (RediSearch)**
- Expresiones de filtro de **AWS DynamoDB**
- Evaluación **en memoria** (LINQ-to-Objects)

Todo construido sobre **Vali-Flow.Core** — un constructor de expresiones ligero, sin dependencias adicionales.

**Plataformas soportadas:** .NET 8.0, .NET 9.0 — todos los paquetes también corren sin modificaciones en **.NET 10** gracias a la compatibilidad hacia adelante de .NET (una librería construida para un TFM anterior funciona bien en un runtime más nuevo).

---

## El problema que resuelve

Cuando se trabaja con múltiples almacenes de datos o patrones de ORM, es habitual dispersar la lógica de filtrado entre repositorios, duplicar predicados por cada almacén, o acoplar la lógica de negocio al código de acceso a datos:

```csharp
// ❌ Enfoque tradicional: la lógica de filtrado está dispersa
public async Task<List<Order>> GetActiveOrdersEF(DbContext db, decimal minTotal)
{
    return await db.Orders
        .Where(o => o.Status == "Active" && o.Total > minTotal)
        .ToListAsync();
}

public List<Order> GetActiveOrdersMongo(IMongoCollection<Order> coll, decimal minTotal)
{
    return coll.Find(Builders<Order>.Filter.And(
        Builders<Order>.Filter.Eq(o => o.Status, "Active"),
        Builders<Order>.Filter.Gt(o => o.Total, minTotal)
    )).ToList();
}

public DataTable GetActiveOrdersSQL(SqlConnection conn, decimal minTotal)
{
    var cmd = new SqlCommand(
        "SELECT * FROM Orders WHERE Status = @status AND Total > @total", conn);
    cmd.Parameters.AddWithValue("@status", "Active");
    cmd.Parameters.AddWithValue("@total", minTotal);
    // ...
}
```

**Con Vali-Flow:**

```csharp
// ✅ Una sola definición de filtro
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, minTotal);

// Usa el mismo filtro en todas partes
var efOrders = await new ValiFlowEvaluator<Order>(dbContext)
    .EvaluateQueryAsync(new BasicSpecification<Order>().WithFilter(filter));

var mongoOrders = mongoCollection.Find(filter.ToMongo()).ToList();

var sqlResult = filter.ToSql(new SqlServerDialect());
var sqlOrders = await connection.QueryAsync<Order>(
    $"SELECT * FROM Orders WHERE {sqlResult.Sql}",
    sqlResult.Parameters);
```

---

## Ecosistema de paquetes

### Paquete principal

| Paquete | Propósito | Versión |
|---------|---------|---------|
| **Vali-Flow.Core** | Constructor de expresiones fluido (`ValiFlow<T>`) - compartido por todos los paquetes | [2.0.0](https://www.nuget.org/packages/Vali-Flow.Core) |
| **Vali-Flow.Abstractions** | Contratos agnósticos de proveedor compartidos por todos los paquetes (`IQueryEvaluator`, `IExpressionTranslator`, `ExpressionInspector`) más `ValiFlowDiagnostics`, un helper de trazabilidad basado en `ActivitySource` compatible con OpenTelemetry | [1.2.0](https://www.nuget.org/packages/Vali-Flow.Abstractions) |

### Paquetes de acceso a datos

| Paquete | Propósito | Destino | Versión |
|---------|---------|--------|---------|
| **Vali-Flow** | Evaluador async de EF Core + especificaciones (lectura/escritura) | `DbContext` | [1.4.0](https://www.nuget.org/packages/Vali-Flow) |
| **Vali-Flow.InMemory** | Evaluador síncrono en memoria para pruebas y caché | `IEnumerable<T>` | [1.2.0](https://www.nuget.org/packages/Vali-Flow.InMemory) |
| **Vali-Flow.Sql** | Constructor de consultas SQL parametrizadas | Dapper / ADO.NET | [1.2.0](https://www.nuget.org/packages/Vali-Flow.Sql) |

### Paquetes NoSQL

| Paquete | Base de datos | Tipo de salida | Versión |
|---------|----------|-------------|---------|
| **Vali-Flow.NoSql** | IR y base de traducción NoSQL compartida, usada por todos los paquetes proveedor listados abajo | — | [1.1.1](https://www.nuget.org/packages/Vali-Flow.NoSql) |
| **Vali-Flow.NoSql.MongoDB** | MongoDB | `BsonDocument` | [1.2.0](https://www.nuget.org/packages/Vali-Flow.NoSql.MongoDB) |
| **Vali-Flow.NoSql.Elasticsearch** | Elasticsearch | `Query` (Elastic.Clients) | [1.2.0](https://www.nuget.org/packages/Vali-Flow.NoSql.Elasticsearch) |
| **Vali-Flow.NoSql.Redis** | Redis (RediSearch) | Cadena de consulta | [1.2.0](https://www.nuget.org/packages/Vali-Flow.NoSql.Redis) |
| **Vali-Flow.NoSql.DynamoDB** | AWS DynamoDB | `DynamoFilterExpression` | [1.2.0](https://www.nuget.org/packages/Vali-Flow.NoSql.DynamoDB) |
| **Vali-Flow.NoSql.Couchbase** | Couchbase | Fragmento WHERE en N1QL + parámetros | [1.1.0](https://www.nuget.org/packages/Vali-Flow.NoSql.Couchbase) |
| **Vali-Flow.NoSql.CosmosDb** | Azure Cosmos DB (SQL API) | Fragmento WHERE en SQL + parámetros | [1.1.0](https://www.nuget.org/packages/Vali-Flow.NoSql.CosmosDb) |
| **Vali-Flow.NoSql.Firestore** | Google Cloud Firestore | `Filter` (Google.Cloud.Firestore) | [1.1.0](https://www.nuget.org/packages/Vali-Flow.NoSql.Firestore) |

### Arquitectura

```
Vali-Flow.Core  (constructor de expresiones — ValiFlow<T>)
       │
       ├─── Vali-Flow                    (EF Core async)
       ├─── Vali-Flow.InMemory           (en memoria, síncrono)
       ├─── Vali-Flow.Sql                (SQL: SQL Server, PostgreSQL, MySQL, SQLite, Oracle)
       │
       └─── Vali-Flow.NoSql
               ├─── Vali-Flow.NoSql.MongoDB        (MongoDB BSON)
               ├─── Vali-Flow.NoSql.Elasticsearch  (Elasticsearch Query DSL)
               ├─── Vali-Flow.NoSql.Redis          (RediSearch)
               ├─── Vali-Flow.NoSql.DynamoDB       (expresiones de filtro de DynamoDB)
               ├─── Vali-Flow.NoSql.Couchbase      (N1QL / SQL++)
               ├─── Vali-Flow.NoSql.CosmosDb       (Cosmos DB SQL API)
               └─── Vali-Flow.NoSql.Firestore      (Filter nativo de Firestore)
```

---

## Inicio rápido

### 1. Definir un filtro una sola vez

```csharp
using Vali_Flow.Core;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m)
    .IsAfter(x => x.CreatedAt, DateTime.UtcNow.AddDays(-30));
```

### 2. Usarlo con EF Core

```csharp
using Vali_Flow;

var evaluator = new ValiFlowEvaluator<Order>(dbContext);
var spec = new BasicSpecification<Order>()
    .WithFilter(filter)
    .WithAsNoTracking(true)
    .AddInclude(x => x.Customer);

var orders = await evaluator.EvaluateQueryAsync(spec, cancellationToken);
```

### 3. O con SQL/Dapper

```csharp
using Vali_Flow.Sql.Extensions;
using Vali_Flow.Sql.Dialects;

var result = filter.ToSql(new PostgreSqlDialect());

var orders = await connection.QueryAsync<Order>(
    $"SELECT * FROM orders WHERE {result.Sql}",
    result.Parameters);
```

### 4. O con MongoDB

```csharp
using Vali_Flow.NoSql.MongoDB.Extensions;

var bsonFilter = filter.ToMongo();
var orders = await collection.Find(bsonFilter).ToListAsync();
```

### 5. O en memoria (pruebas)

```csharp
using Vali_Flow.InMemory;

var evaluator = new ValiFlowEvaluator<Order, int>(orders, null, x => x.Id);
var filtered = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    valiFlow: filter);
```

---

## Instalación

**Instala el o los paquetes que necesites:**

```bash
# EF Core (producción)
dotnet add package Vali-Flow

# En memoria (pruebas / caché)
dotnet add package Vali-Flow.InMemory

# Consultas SQL (Dapper / ADO.NET)
dotnet add package Vali-Flow.Sql

# MongoDB
dotnet add package Vali-Flow.NoSql.MongoDB

# Elasticsearch
dotnet add package Vali-Flow.NoSql.Elasticsearch

# Redis (RediSearch)
dotnet add package Vali-Flow.NoSql.Redis

# AWS DynamoDB
dotnet add package Vali-Flow.NoSql.DynamoDB
```

Todos los paquetes incluyen automáticamente **Vali-Flow.Core** como dependencia transitiva.

---

## Características principales

### DSL de filtros fluido (`ValiFlow<T>`)

Construye filtros complejos con una API natural y encadenable:

```csharp
var filter = new ValiFlow<Product>()
    // Comparación
    .EqualTo(x => x.Category, "Electronics")
    .GreaterThanOrEqualTo(x => x.Price, 100m)
    // Operaciones de cadena
    .Contains(x => x.Name, "phone")
    .StartsWith(x => x.Sku, "PROD")
    // Rangos numéricos
    .Between(x => x.Quantity, 1, 1000)
    // Fechas
    .IsAfter(x => x.CreatedAt, DateTime.UtcNow.AddDays(-90))
    // Colección
    .NotEmpty(x => x.Reviews)
    // Booleano
    .IsTrue(x => x.IsActive)
    // Operadores lógicos
    .Or()
    .EqualTo(x => x.Category, "Accessories");
```

Consulta [Vali-Flow.Core](https://github.com/UBF21/vali-flow-core) para la lista completa de más de 50 predicados.

### Especificaciones

Encapsulan criterios de consulta, ordenamiento, paginación y carga ansiosa (eager loading):

```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(filter)
    .WithOrderBy(x => x.CreatedAt, ascending: false)
    .AddThenBy(x => x.Total, ascending: true)
    .WithPagination(page: 1, pageSize: 20)
    .AddInclude(x => x.Customer)
    .AddInclude(x => x.OrderLines)
    .WithAsNoTracking(true);
```

### EF Core: operaciones de lectura

```csharp
var evaluator = new ValiFlowEvaluator<Order>(dbContext);

// Existencia y conteo
bool exists = await evaluator.EvaluateAnyAsync(spec);
int count   = await evaluator.EvaluateCountAsync(spec);

// Entidades individuales
Order? first = await evaluator.EvaluateGetFirstAsync(spec);
Order? last  = await evaluator.EvaluateGetLastAsync(spec);

// Consulta completa
IQueryable<Order> query = await evaluator.EvaluateQueryAsync(spec);

// Distintos y duplicados
IQueryable<Order> distinct   = await evaluator.EvaluateDistinctAsync(spec, x => x.CustomerId);
IQueryable<Order> duplicates = await evaluator.EvaluateDuplicatesAsync(spec, x => x.CustomerId);

// Agregados
decimal minTotal = await evaluator.EvaluateMinAsync(spec, x => x.Total);
decimal maxTotal = await evaluator.EvaluateMaxAsync(spec, x => x.Total);
decimal avgTotal = await evaluator.EvaluateAverageAsync(spec, x => x.Total);
decimal sumTotal = await evaluator.EvaluateSumAsync(spec, x => x.Total);

// Agregados agrupados
Dictionary<string, int> countByStatus = 
    await evaluator.EvaluateCountByGroupAsync(spec, x => x.Status);

Dictionary<string, decimal> sumByStatus = 
    await evaluator.EvaluateSumByGroupAsync(spec, x => x.Status, x => x.Total);
```

### EF Core: operaciones de escritura

```csharp
var evaluator = new ValiFlowEvaluator<Order>(dbContext);

// Entidad individual
var added   = await evaluator.AddAsync(order, saveChanges: true);
var updated = await evaluator.UpdateAsync(order, saveChanges: true);
await evaluator.DeleteAsync(order, saveChanges: true);

// Por lotes
await evaluator.AddRangeAsync(orders);
await evaluator.UpdateRangeAsync(orders);
await evaluator.DeleteRangeAsync(orders);

// Eliminación condicional
await evaluator.DeleteByConditionAsync(
    condition: x => x.Status == "Expired" && x.CreatedAt < cutoffDate);

// Upsert (inserta si no existe, actualiza si existe)
var upserted = await evaluator.UpsertAsync(
    entity: order,
    matchCondition: x => x.Id == order.Id);

// Operaciones masivas (vía EFCore.BulkExtensions)
await evaluator.BulkInsertAsync(orders, new BulkConfig { BatchSize = 5000 });
await evaluator.BulkUpdateAsync(orders, new BulkConfig { BatchSize = 5000 });
await evaluator.BulkInsertOrUpdateAsync(orders);

// Transacciones
await evaluator.ExecuteTransactionAsync(async () =>
{
    await evaluator.AddAsync(order1, saveChanges: false);
    await evaluator.UpdateAsync(order2, saveChanges: false);
    await evaluator.SaveChangesAsync();
});
```

### Constructor de consultas SQL (Dapper / ADO.NET)

Cuatro dialectos listos para usar: SQL Server, PostgreSQL, MySQL, SQLite.

```csharp
// Cláusula WHERE simple
var result = filter.ToSql(new PostgreSqlDialect());
var orders = await connection.QueryAsync<Order>(
    $"SELECT * FROM orders WHERE {result.Sql}",
    result.Parameters);

// SELECT completo con JOIN, GROUP BY, agregados
var query = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .Select(x => x.Id, x => x.Status, x => x.Total)
    .From("orders")
    .Where(w => w.EqualTo(x => x.Status, "Active"))
    .OrderBy(x => x.CreatedAt, ascending: false)
    .Paginate(page: 1, pageSize: 20);

var result = query.Build();
```

### Evaluador en memoria (pruebas / caché)

Evaluación síncrona y sin dependencias contra `IEnumerable<T>`:

```csharp
var evaluator = new ValiFlowEvaluator<Order, int>(orders, null, x => x.Id);

var filter = new ValiFlow<Order>().EqualTo(x => x.Status, "Active");

int count  = evaluator.EvaluateCount(orders, filter);
Order? first = evaluator.GetFirst(orders, filter);

IEnumerable<Order> filtered = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    valiFlow: filter);

Dictionary<string, int> countByStatus = 
    evaluator.EvaluateCountByGroup(orders, x => x.Status, filter);
```

---

## Soporte NoSQL

### MongoDB

```csharp
using Vali_Flow.NoSql.MongoDB.Extensions;

var filter = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Age, 18);

BsonDocument bsonFilter = filter.ToMongo();
var users = await collection.Find(bsonFilter).ToListAsync();
```

### Elasticsearch

```csharp
using Vali_Flow.NoSql.Elasticsearch.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.Category, "Electronics")
    .GreaterThanOrEqualTo(x => x.Price, 100m)
    .Contains(x => x.Name, "phone");

Query esQuery = filter.ToElasticsearch();
var response = await client.SearchAsync<Product>(s => s.Query(esQuery));
```

### Redis (RediSearch)

```csharp
using Vali_Flow.NoSql.Redis.Extensions;

string redisQuery = filter.ToRedisSearch();
var results = db.FT().Search("idx:products", new Query(redisQuery));
```

### DynamoDB

```csharp
using Vali_Flow.NoSql.DynamoDB.Extensions;

DynamoFilterExpression f = filter.ToDynamoDB();

var request = new ScanRequest
{
    TableName                 = "Orders",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};
```

---

## Documentación

- **[Guía completa de funcionalidades](docs/FEATURES.md)** — Ejemplos detallados de cada paquete
- **[Guía de arquitectura](docs/ARCHITECTURE.md)** — Patrones de diseño y justificación de decisiones
- **[Referencia de dialectos SQL](Vali-Flow.Sql/README.md)** — Capacidades del SQL Builder
- **[Vali-Flow.Core](https://github.com/UBF21/vali-flow-core)** — Predicados del constructor de expresiones

---

## Licencia

Distribuido bajo la [Licencia MIT](LICENSE).  
Copyright © 2025 Felipe Rafael Montenegro Morriberon. Todos los derechos reservados.

---

## Soporte

- **Issues y solicitudes de funcionalidades:** [GitHub Issues](https://github.com/UBF21/vali-flow/issues)
- **Discusiones:** [GitHub Discussions](https://github.com/UBF21/vali-flow/discussions)

### Contribuir

¡Las contribuciones son bienvenidas! Consulta [CONTRIBUTING.md](CONTRIBUTING.md) para las pautas.

Si este proyecto te resulta útil, considera apoyar su desarrollo:
- **Latinoamérica** — [MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **Internacional** — [PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)
