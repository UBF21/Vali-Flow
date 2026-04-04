# Vali-Flow — Evaluador EF Core

## Tabla de contenidos

1. [Descripcion general](#descripcion-general)
2. [Configuracion](#configuracion)
3. [Especificaciones](#especificaciones)
   - [BasicSpecification\<T\>](#basicspecificationt)
   - [QuerySpecification\<T\>](#queryspecificationt)
4. [Metodos de lectura](#metodos-de-lectura)
   - [negateCondition explicado](#negatecondition-explicado)
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
5. [Metodos de escritura](#metodos-de-escritura)
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
   - [BulkInsertOrUpdateAsync](#bulkinsertorjpdateasync)

---

## Descripcion general

**Vali-Flow** es una biblioteca .NET que simplifica el acceso a datos en aplicaciones Entity Framework Core mediante una API de especificaciones fluida. Desacopla los criterios de consulta de la capa de acceso a datos, produciendo codigo limpio, componible y testeable.

Caracteristicas principales:

- Patron de especificacion con filtro, ordenamiento, paginacion y hints de EF Core
- API completamente asincrona sobre EF Core
- Operaciones masivas (bulk) mediante `EFCore.BulkExtensions`
- Guardado diferido para agrupar varias operaciones de escritura
- Filtros invertidos con variantes "Failed" sin modificar la especificacion

**Instalacion**

```bash
dotnet add package Vali-Flow
```

**Requisitos**

- .NET 8 o superior
- Entity Framework Core 8 o superior

---

## Configuracion

`ValiFlowEvaluator<T>` es una clase generica sellada que recibe un `DbContext` e implementa tanto `IEvaluatorRead<T>` como `IEvaluatorWrite<T>`.

**Constructor**

```csharp
public ValiFlowEvaluator<T>(DbContext dbContext)
```

**Inyeccion de dependencias (recomendado)**

Registrar un evaluador tipado por entidad en el contenedor de DI:

```csharp
// Program.cs / Startup.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<ValiFlowEvaluator<Order>>(sp =>
    new ValiFlowEvaluator<Order>(sp.GetRequiredService<AppDbContext>()));
```

**Instanciacion manual**

```csharp
var evaluator = new ValiFlowEvaluator<Order>(dbContext);
```

> **Nota:** El evaluador mantiene una referencia al `DbContext` proporcionado. Seguir las reglas de ciclo de vida estandar de EF Core — inyectar un `DbContext` con scope en aplicaciones web.

---

## Especificaciones

Las especificaciones encapsulan todos los criterios de consulta en un unico objeto reutilizable. Hay dos clases de especificacion:

| Clase | Hereda de | Usar cuando |
|-------|-----------|-------------|
| `BasicSpecification<T>` | — | Solo filtrado, includes y hints de EF Core |
| `QuerySpecification<T>` | `BasicSpecification<T>` | Ordenamiento, paginacion, top o ValiSort |

### BasicSpecification\<T\>

`BasicSpecification<T>` es la clase base. Se configura mediante sus metodos fluidos:

| Metodo | Descripcion |
|--------|-------------|
| `WithFilter(ValiFlow<T>)` | Establece la expresion de filtro |
| `WithIncludes(...)` | Agrega includes de carga ansiosa (eager loading) |
| `AsNoTracking()` | Desactiva el seguimiento de cambios de EF Core |
| `AsSplitQuery()` | Activa consultas divididas para colecciones |
| `IgnoreQueryFilters()` | Omite los filtros globales de consulta |

```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .AsNoTracking();
```

### QuerySpecification\<T\>

`QuerySpecification<T>` extiende `BasicSpecification<T>` con:

| Metodo | Descripcion |
|--------|-------------|
| `WithOrderBy<TProperty>(expr, ascending)` | Ordenamiento principal |
| `AddThenBy<TProperty>(expr, ascending)` | Ordenamiento secundario (requiere `WithOrderBy` primero) |
| `AddThenBys<TProperty>(exprs, ascending)` | Multiples ordenamientos secundarios a la vez |
| `WithPagination(page, pageSize)` | Establece pagina y tamano de pagina juntos |
| `WithPage(page)` | Establece solo el numero de pagina |
| `WithPageSize(pageSize)` | Establece solo el tamano de pagina |
| `WithTop(count)` | Limita el resultado a los primeros N elementos |
| `WithValiSort(ValiSort<T>)` | Ordenamiento dinamico mediante reflexion |

**Reglas de exclusion mutua**

- `WithOrderBy` y `WithValiSort` no pueden combinarse.
- `WithTop` y `WithPagination` / `WithPage` / `WithPageSize` no pueden combinarse.
- `AddThenBy` requiere que `WithOrderBy` haya sido llamado primero.

**Ejemplo — especificacion combinada**

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

**Ejemplo — constructor con filtro**

```csharp
var filter = new ValiFlow<Product>().LessThan(p => p.Stock, 10);
var spec = new QuerySpecification<Product>(filter)
    .WithOrderBy(p => p.Name)
    .WithTop(5);
```

**Ejemplo — ValiSort (ordenamiento dinamico)**

```csharp
var sort = new ValiSort<Order>().By("Total", ascending: false);
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithValiSort(sort);
```

---

## Metodos de lectura

Todos los metodos de lectura son asincronos y operan sobre el `DbContext` para el tipo de entidad `T`. Las especificaciones se pasan por interfaz (`IBasicSpecification<T>` o `IQuerySpecification<T>`), por lo que tanto `BasicSpecification<T>` como `QuerySpecification<T>` son aceptadas donde se espera la interfaz base.

### negateCondition explicado

Varios metodos de lectura exponen la negacion del filtro a traves de variantes "Failed". Cuando la consulta se construye con el filtro negado, la expresion `ValiFlow<T>` se invierte logicamente: las entidades que normalmente serian **excluidas** pasan a estar **incluidas**, y viceversa.

Esto permite reutilizar la misma especificacion para consultar entidades que pasan y entidades que fallan el filtro:

```csharp
var activeFilter = new ValiFlow<User>().IsTrue(u => u.IsActive);
var spec = new BasicSpecification<User>().WithFilter(activeFilter);

// Retorna usuarios activos
bool hayActivos = await evaluator.EvaluateAnyAsync(spec);

// Las variantes "Failed" niegan automaticamente el filtro
User? primerInactivo = await evaluator.EvaluateGetFirstFailedAsync(spec);
```

---

### `EvaluateAsync`

**Firma**
```csharp
Task<bool> EvaluateAsync(ValiFlow<T> valiFlow, T entity)
```

**Descripcion**
Evalua si una entidad en memoria satisface la condicion `ValiFlow<T>` indicada. No realiza ninguna consulta a la base de datos. Util para validar entidades antes de guardarlas.

**Ejemplo**
```csharp
var rule = new ValiFlow<Order>()
    .GreaterThan(o => o.Total, 0m)
    .IsNotNull(o => o.CustomerId);

var order = new Order { Total = 150m, CustomerId = "C001" };
bool esValido = await evaluator.EvaluateAsync(rule, order);
```

> **Nota:** Es el unico metodo de lectura que no consulta la base de datos.

---

### `EvaluateAnyAsync`

**Firma**
```csharp
Task<bool> EvaluateAnyAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default)
```

**Descripcion**
Retorna `true` si al menos una entidad en la base de datos satisface el filtro de la especificacion. Se traduce a una verificacion SQL de tipo `EXISTS` o `COUNT`.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().LessThan(p => p.Stock, 5));

bool hayStockBajo = await evaluator.EvaluateAnyAsync(spec);
```

---

### `EvaluateCountAsync`

**Firma**
```csharp
Task<int> EvaluateCountAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default)
```

**Descripcion**
Retorna el total de entidades que satisfacen el filtro de la especificacion.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .Equal(o => o.Status, OrderStatus.Pending));

int cantidadPendientes = await evaluator.EvaluateCountAsync(spec);
```

---

### `EvaluateGetFirstAsync`

**Firma**
```csharp
Task<T?> EvaluateGetFirstAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default)
```

**Descripcion**
Retorna la primera entidad que satisface la especificacion, o `null` si ninguna coincide. Equivalente a `FirstOrDefaultAsync` con el filtro aplicado.

**Ejemplo**
```csharp
var spec = new BasicSpecification<User>()
    .WithFilter(new ValiFlow<User>().Equal(u => u.Email, "admin@example.com"));

User? admin = await evaluator.EvaluateGetFirstAsync(spec);
```

---

### `EvaluateGetLastAsync`

**Firma**
```csharp
Task<T?> EvaluateGetLastAsync(IQuerySpecification<T> specification, CancellationToken cancellationToken = default)
```

**Descripcion**
Retorna la ultima entidad que satisface la especificacion segun el ordenamiento definido. Retorna `null` si ninguna coincide.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.CreatedAt);

Order? ultimoPedido = await evaluator.EvaluateGetLastAsync(spec);
```

> **Nota:** Se requiere un ordenamiento principal (`WithOrderBy`). Las bases de datos SQL no tienen un concepto de "ultimo" sin `ORDER BY`.

---

### `EvaluateGetFirstFailedAsync`

**Firma**
```csharp
Task<T?> EvaluateGetFirstFailedAsync(IBasicSpecification<T> specification, CancellationToken cancellationToken = default)
```

**Descripcion**
Retorna la primera entidad que **no** satisface el filtro de la especificacion, o `null` si todas las entidades pasan. Aplica la negacion logica del filtro.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsAvailable));

Product? noDisponible = await evaluator.EvaluateGetFirstFailedAsync(spec);
```

---

### `EvaluateGetLastFailedAsync`

**Firma**
```csharp
Task<T?> EvaluateGetLastFailedAsync(IQuerySpecification<T> specification, CancellationToken cancellationToken = default)
```

**Descripcion**
Retorna la ultima entidad que **no** satisface el filtro de la especificacion, segun el ordenamiento definido. Retorna `null` si todas las entidades pasan.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsPaid))
    .WithOrderBy(o => o.CreatedAt);

Order? ultimoSinPagar = await evaluator.EvaluateGetLastFailedAsync(spec);
```

---

### `EvaluateQueryAsync`

**Firma**
```csharp
Task<IQueryable<T>> EvaluateQueryAsync(IQuerySpecification<T> specification)
```

**Descripcion**
Retorna un `IQueryable<T>` con la especificacion completa aplicada: filtro, ordenamiento, paginacion, includes y hints de EF Core. La consulta no se ejecuta de inmediato; se puede seguir componiendo o materializar con `ToListAsync`, `ToArrayAsync`, etc.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.CreatedAt, ascending: false)
    .WithPagination(page: 1, pageSize: 10)
    .AsNoTracking();

IQueryable<Order> query = await evaluator.EvaluateQueryAsync(spec);
List<Order> pedidos = await query.ToListAsync();
```

---

### `EvaluateQueryFailedAsync`

**Firma**
```csharp
Task<IQueryable<T>> EvaluateQueryFailedAsync(IQuerySpecification<T> specification)
```

**Descripcion**
Retorna un `IQueryable<T>` para entidades que **no** satisfacen el filtro de la especificacion, con ordenamiento y paginacion aplicados.

**Ejemplo**
```csharp
var spec = new QuerySpecification<User>()
    .WithFilter(new ValiFlow<User>().IsTrue(u => u.EmailConfirmed))
    .WithOrderBy(u => u.CreatedAt);

IQueryable<User> query = await evaluator.EvaluateQueryFailedAsync(spec);
List<User> sinConfirmar = await query.ToListAsync();
```

---

### `EvaluateAllAsync`

**Firma**
```csharp
Task<IQueryable<T>> EvaluateAllAsync(IQuerySpecification<T> specification)
```

**Descripcion**
Alias de `EvaluateQueryAsync`. Retorna todas las entidades que satisfacen la especificacion. Proporcionado para simetria de API con el paquete `Vali-Flow.InMemory`.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsActive))
    .WithOrderBy(p => p.Name);

var query = await evaluator.EvaluateAllAsync(spec);
var productos = await query.ToListAsync();
```

---

### `EvaluateAllFailedAsync`

**Firma**
```csharp
Task<IQueryable<T>> EvaluateAllFailedAsync(IQuerySpecification<T> specification)
```

**Descripcion**
Alias de `EvaluateQueryFailedAsync`. Retorna todas las entidades que no satisfacen la especificacion. Proporcionado para simetria de API con el paquete `Vali-Flow.InMemory`.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Product>()
    .WithFilter(new ValiFlow<Product>().GreaterThan(p => p.Stock, 0))
    .WithOrderBy(p => p.Name);

var query = await evaluator.EvaluateAllFailedAsync(spec);
var sinStock = await query.ToListAsync();
```

---

### `EvaluateTopAsync`

**Firma**
```csharp
Task<IQueryable<T>> EvaluateTopAsync(IQuerySpecification<T> specification, int count, CancellationToken cancellationToken = default)
```

**Descripcion**
Retorna un `IQueryable<T>` limitado a las primeras `count` entidades que satisfacen la especificacion, con el ordenamiento aplicado.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.Total, ascending: false);

IQueryable<Order> topPedidos = await evaluator.EvaluateTopAsync(spec, count: 5);
var resultado = await topPedidos.ToListAsync();
```

> **Nota:** `count` debe ser mayor que cero.

---

### `EvaluateDistinctAsync`

**Firma**
```csharp
Task<IEnumerable<T>> EvaluateDistinctAsync<TKey>(
    IQuerySpecification<T> specification,
    Expression<Func<T, TKey>> selector
) where TKey : notnull
```

**Descripcion**
Retorna un conjunto de entidades distintas agrupadas por el selector de clave. Por cada grupo, solo se retorna la primera entidad. El ordenamiento y la paginacion de la especificacion se aplican luego de la deduplicacion.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.CreatedAt);

// Un pedido por cliente
IEnumerable<Order> distintos = await evaluator.EvaluateDistinctAsync(
    spec, o => o.CustomerId);
```

---

### `EvaluateDuplicatesAsync`

**Firma**
```csharp
Task<IEnumerable<T>> EvaluateDuplicatesAsync<TKey>(
    IQuerySpecification<T> specification,
    Expression<Func<T, TKey>> selector
) where TKey : notnull
```

**Descripcion**
Retorna todas las entidades pertenecientes a grupos donde la clave aparece mas de una vez, es decir, entradas duplicadas segun la clave indicada.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsActive));

// Productos que comparten el mismo SKU
IEnumerable<Product> duplicados = await evaluator.EvaluateDuplicatesAsync(
    spec, p => p.Sku);
```

---

### `EvaluatePagedAsync`

**Firma**
```csharp
Task<PagedResult<T>> EvaluatePagedAsync(
    IQuerySpecification<T> specification,
    CancellationToken cancellationToken = default
)
```

**Descripcion**
Ejecuta la consulta y retorna un `PagedResult<T>` que contiene los elementos de la pagina solicitada junto con metadatos de paginacion (total de registros, total de paginas, pagina actual, tamano de pagina).

**Ejemplo**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.CreatedAt, ascending: false)
    .WithPagination(page: 1, pageSize: 20)
    .AsNoTracking();

PagedResult<Order> paginado = await evaluator.EvaluatePagedAsync(spec);

Console.WriteLine($"Pagina {paginado.Page} de {paginado.TotalPages} ({paginado.TotalCount} total)");
foreach (var order in paginado.Items)
{
    Console.WriteLine(order.Id);
}
```

---

### `EvaluateMinAsync`

**Firma**
```csharp
Task<TResult> EvaluateMinAsync<TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TResult>> selector,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult>
```

**Descripcion**
Retorna el valor minimo de la propiedad numerica seleccionada entre las entidades que satisfacen la especificacion.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

decimal totalMinimo = await evaluator.EvaluateMinAsync(spec, o => o.Total);
```

---

### `EvaluateMaxAsync`

**Firma**
```csharp
Task<TResult> EvaluateMaxAsync<TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TResult>> selector,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult>
```

**Descripcion**
Retorna el valor maximo de la propiedad numerica seleccionada entre las entidades que satisfacen la especificacion.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsAvailable));

decimal precioMaximo = await evaluator.EvaluateMaxAsync(spec, p => p.Price);
```

---

### `EvaluateAverageAsync`

**Firma**
```csharp
Task<decimal> EvaluateAverageAsync<TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TResult>> selector,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult>
```

**Descripcion**
Retorna el promedio aritmetico (como `decimal`) de la propiedad numerica seleccionada entre las entidades que satisfacen la especificacion.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsPaid));

decimal promedioTotal = await evaluator.EvaluateAverageAsync(spec, o => o.Total);
```

---

### `EvaluateSumAsync`

**Firma**
```csharp
// Sobrecargas para int, long, double, decimal, float
Task<int>     EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, int>>, CancellationToken = default)
Task<long>    EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, long>>, CancellationToken = default)
Task<double>  EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, double>>, CancellationToken = default)
Task<decimal> EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, decimal>>, CancellationToken = default)
Task<float>   EvaluateSumAsync(IBasicSpecification<T>, Expression<Func<T, float>>, CancellationToken = default)
```

**Descripcion**
Retorna la suma de la propiedad numerica seleccionada para todas las entidades que satisfacen la especificacion. Cinco sobrecargas cubren los tipos `int`, `long`, `double`, `decimal` y `float`.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .Equal(o => o.Status, OrderStatus.Completed));

decimal ingresoTotal = await evaluator.EvaluateSumAsync(spec, o => o.Total);
int totalArticulos = await evaluator.EvaluateSumAsync(spec, o => o.ItemCount);
```

---

### `EvaluateAggregateAsync`

**Firma**
```csharp
Task<TResult> EvaluateAggregateAsync<TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TResult>> selector,
    Func<TResult, TResult, TResult> aggregator,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult>
```

**Descripcion**
Aplica una funcion de agregacion personalizada sobre los valores de la propiedad seleccionada. Util para agregaciones no cubiertas por los metodos integrados Min/Max/Sum/Average.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

// Producto de todos los totales (agregacion personalizada)
decimal producto = await evaluator.EvaluateAggregateAsync(
    spec,
    o => o.Total,
    (acumulado, valor) => acumulado * valor);
```

---

### `EvaluateGroupedAsync`

**Firma**
```csharp
Task<Dictionary<TKey, List<T>>> EvaluateGroupedAsync<TKey>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Descripcion**
Agrupa todas las entidades que satisfacen la especificacion segun el selector de clave indicado y retorna un diccionario que mapea cada clave a la lista de entidades de ese grupo.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

Dictionary<string, List<Order>> porCliente =
    await evaluator.EvaluateGroupedAsync(spec, o => o.CustomerId);
```

---

### `EvaluateCountByGroupAsync`

**Firma**
```csharp
Task<Dictionary<TKey, int>> EvaluateCountByGroupAsync<TKey>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Descripcion**
Retorna un diccionario que mapea cada clave de grupo al conteo de entidades en ese grupo.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

Dictionary<OrderStatus, int> cantidadPorEstado =
    await evaluator.EvaluateCountByGroupAsync(spec, o => o.Status);
```

---

### `EvaluateSumByGroupAsync`

**Firma**
```csharp
// Sobrecargas para int, long, float, double, decimal
Task<Dictionary<TKey, int>>     EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, int>>, ct)
Task<Dictionary<TKey, long>>    EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, long>>, ct)
Task<Dictionary<TKey, float>>   EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, float>>, ct)
Task<Dictionary<TKey, double>>  EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, double>>, ct)
Task<Dictionary<TKey, decimal>> EvaluateSumByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, decimal>>, ct)
```

**Descripcion**
Agrupa las entidades por el selector de clave y retorna un diccionario que mapea cada clave a la suma de la propiedad numerica seleccionada dentro de ese grupo. Cinco sobrecargas cubren los principales tipos numericos.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsPaid));

Dictionary<string, decimal> ingresosPorCliente =
    await evaluator.EvaluateSumByGroupAsync(spec, o => o.CustomerId, o => o.Total);
```

---

### `EvaluateMinByGroupAsync`

**Firma**
```csharp
// Sobrecargas para int, long, float, double, decimal
Task<Dictionary<TKey, int>>     EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, int>>, ct)
Task<Dictionary<TKey, long>>    EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, long>>, ct)
Task<Dictionary<TKey, float>>   EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, float>>, ct)
Task<Dictionary<TKey, double>>  EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, double>>, ct)
Task<Dictionary<TKey, decimal>> EvaluateMinByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, decimal>>, ct)
```

**Descripcion**
Agrupa las entidades por el selector de clave y retorna el valor minimo de la propiedad numerica seleccionada por grupo. Retorna `default(TValue)` para grupos vacios.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsActive));

Dictionary<string, decimal> precioMinimoPorCategoria =
    await evaluator.EvaluateMinByGroupAsync(spec, p => p.Category, p => p.Price);
```

---

### `EvaluateMaxByGroupAsync`

**Firma**
```csharp
// Sobrecargas para int, long, float, double, decimal
Task<Dictionary<TKey, int>>     EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, int>>, ct)
Task<Dictionary<TKey, long>>    EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, long>>, ct)
Task<Dictionary<TKey, float>>   EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, float>>, ct)
Task<Dictionary<TKey, double>>  EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, double>>, ct)
Task<Dictionary<TKey, decimal>> EvaluateMaxByGroupAsync<TKey>(spec, keySelector, Expression<Func<T, decimal>>, ct)
```

**Descripcion**
Agrupa las entidades por el selector de clave y retorna el valor maximo de la propiedad numerica seleccionada por grupo. Retorna `default(TValue)` para grupos vacios.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive));

Dictionary<string, decimal> maxPedidoPorCliente =
    await evaluator.EvaluateMaxByGroupAsync(spec, o => o.CustomerId, o => o.Total);
```

---

### `EvaluateAverageByGroupAsync`

**Firma**
```csharp
Task<Dictionary<TKey, decimal>> EvaluateAverageByGroupAsync<TKey, TResult>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    Expression<Func<T, TResult>> selector,
    CancellationToken cancellationToken = default
) where TResult : INumber<TResult> where TKey : notnull
```

**Descripcion**
Agrupa las entidades por el selector de clave y retorna el promedio (como `decimal`) de la propiedad numerica seleccionada por grupo.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsPaid));

Dictionary<string, decimal> promedioPorCliente =
    await evaluator.EvaluateAverageByGroupAsync(spec, o => o.CustomerId, o => o.Total);
```

---

### `EvaluateDuplicatesByGroupAsync`

**Firma**
```csharp
Task<Dictionary<TKey, List<T>>> EvaluateDuplicatesByGroupAsync<TKey>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Descripcion**
Retorna un diccionario que mapea cada clave duplicada a la lista de entidades que la comparten. Solo se incluyen los grupos con mas de una entidad.

**Ejemplo**
```csharp
var spec = new BasicSpecification<Product>()
    .WithFilter(new ValiFlow<Product>().IsTrue(p => p.IsActive));

Dictionary<string, List<Product>> duplicadosPorSku =
    await evaluator.EvaluateDuplicatesByGroupAsync(spec, p => p.Sku);
```

---

### `EvaluateUniquesByGroupAsync`

**Firma**
```csharp
Task<Dictionary<TKey, T>> EvaluateUniquesByGroupAsync<TKey>(
    IBasicSpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Descripcion**
Retorna un diccionario que mapea cada clave a una unica entidad. Solo se incluyen los grupos con exactamente una entidad; los grupos con duplicados son excluidos.

**Ejemplo**
```csharp
var spec = new BasicSpecification<User>()
    .WithFilter(new ValiFlow<User>().IsTrue(u => u.IsActive));

Dictionary<string, User> unicosPorEmail =
    await evaluator.EvaluateUniquesByGroupAsync(spec, u => u.Email);
```

---

### `EvaluateTopByGroupAsync`

**Firma**
```csharp
Task<Dictionary<TKey, List<T>>> EvaluateTopByGroupAsync<TKey>(
    IQuerySpecification<T> specification,
    Expression<Func<T, TKey>> keySelector,
    CancellationToken cancellationToken = default
) where TKey : notnull
```

**Descripcion**
Obtiene las primeras N entidades (segun `WithTop` en la especificacion) y las agrupa por el selector de clave. Si `Top` no esta definido, se usa 50 entidades por defecto. Las propiedades de paginacion son ignoradas.

**Ejemplo**
```csharp
var spec = new QuerySpecification<Order>()
    .WithFilter(new ValiFlow<Order>().IsTrue(o => o.IsActive))
    .WithOrderBy(o => o.Total, ascending: false)
    .WithTop(10);

Dictionary<string, List<Order>> top10PorCliente =
    await evaluator.EvaluateTopByGroupAsync(spec, o => o.CustomerId);
```

---

## Metodos de escritura

Todos los metodos de escritura son asincronos. La mayoria acepta el parametro `saveChanges` (por defecto `true`) que controla si se llama a `SaveChangesAsync` de inmediato. Pasar `saveChanges: false` permite agrupar varias operaciones y llamar a `SaveChangesAsync()` una sola vez al final.

### Patron de guardado diferido

```csharp
var order = new Order { CustomerId = "C001", Total = 250m };
var item1 = new OrderItem { ProductId = "P1", Quantity = 2 };
var item2 = new OrderItem { ProductId = "P2", Quantity = 1 };

await orderEvaluator.AddAsync(order, saveChanges: false);
await itemEvaluator.AddAsync(item1, saveChanges: false);
await itemEvaluator.AddAsync(item2, saveChanges: false);

await orderEvaluator.SaveChangesAsync(); // un unico round-trip a la base de datos
```

---

### `AddAsync`

**Firma**
```csharp
Task<T> AddAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Descripcion**
Agrega una entidad a la base de datos. Retorna la entidad agregada (con valores generados por la base de datos disponibles si `saveChanges: true`).

**Ejemplo**
```csharp
var product = new Product { Name = "Widget", Price = 9.99m, Stock = 100 };
Product guardado = await evaluator.AddAsync(product);
Console.WriteLine(guardado.Id); // ID autogenerado disponible
```

> **Nota:** Lanza `ArgumentNullException` si `entity` es null.

---

### `AddRangeAsync`

**Firma**
```csharp
Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Descripcion**
Agrega una coleccion de entidades a la base de datos en una sola operacion. Retorna las entidades agregadas.

**Ejemplo**
```csharp
var products = new List<Product>
{
    new Product { Name = "Widget A", Price = 9.99m },
    new Product { Name = "Widget B", Price = 14.99m },
};

IEnumerable<Product> guardados = await evaluator.AddRangeAsync(products);
```

> **Nota:** Lanza `ArgumentException` si `entities` esta vacio.

---

### `UpdateAsync`

**Firma**
```csharp
Task<T> UpdateAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Descripcion**
Marca una entidad como modificada y persiste los cambios en la base de datos. La entidad debe estar rastreada por EF Core o tener su clave primaria establecida.

**Ejemplo**
```csharp
var order = await evaluator.EvaluateGetFirstAsync(spec);
order.Status = OrderStatus.Shipped;

Order actualizado = await evaluator.UpdateAsync(order);
```

---

### `UpdateRangeAsync`

**Firma**
```csharp
Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Descripcion**
Marca una coleccion de entidades como modificadas y persiste todos los cambios en una sola llamada.

**Ejemplo**
```csharp
var orders = (await evaluator.EvaluateQueryAsync(spec)).ToList();
orders.ForEach(o => o.Status = OrderStatus.Processing);

await evaluator.UpdateRangeAsync(orders);
```

---

### `DeleteAsync`

**Firma**
```csharp
Task DeleteAsync(T entity, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Descripcion**
Elimina una entidad de la base de datos. La entidad debe estar rastreada o adjunta antes de la eliminacion.

**Ejemplo**
```csharp
var product = await evaluator.EvaluateGetFirstAsync(spec);
if (product is not null)
    await evaluator.DeleteAsync(product);
```

---

### `DeleteRangeAsync`

**Firma**
```csharp
Task DeleteRangeAsync(IEnumerable<T> entities, bool saveChanges = true, CancellationToken cancellationToken = default)
```

**Descripcion**
Elimina una coleccion de entidades de la base de datos en una sola llamada.

**Ejemplo**
```csharp
var pedidosVencidos = await (await evaluator.EvaluateQueryAsync(spec)).ToListAsync();
await evaluator.DeleteRangeAsync(pedidosVencidos);
```

---

### `DeleteByConditionAsync`

**Firma**
```csharp
Task DeleteByConditionAsync(Expression<Func<T, bool>> condition, CancellationToken cancellationToken = default)
```

**Descripcion**
Emite un `DELETE` SQL directo para todas las entidades que satisfacen la condicion sin cargarlas en memoria. Usa `ExecuteDeleteAsync` de EF Core 7+. No tiene parametro `saveChanges` — el borrado se aplica de inmediato.

**Ejemplo**
```csharp
await evaluator.DeleteByConditionAsync(o => o.CreatedAt < DateTime.UtcNow.AddYears(-2));
```

> **Nota:** No activa el seguimiento de cambios ni los eventos de entidad de EF Core. Usar con precaucion en configuraciones de borrado logico (soft-delete).

---

### `SaveChangesAsync`

**Firma**
```csharp
Task SaveChangesAsync(CancellationToken cancellationToken = default)
```

**Descripcion**
Vuelca todos los cambios pendientes del `DbContext` en la base de datos. Usar despues de agrupar varias operaciones con `saveChanges: false`.

**Ejemplo**
```csharp
await evaluator.AddAsync(entidad1, saveChanges: false);
await evaluator.UpdateAsync(entidad2, saveChanges: false);
await evaluator.SaveChangesAsync();
```

---

### `UpsertAsync`

**Firma**
```csharp
Task<T> UpsertAsync(
    T entity,
    Expression<Func<T, bool>> matchCondition,
    bool saveChanges = true,
    CancellationToken cancellationToken = default)
```

**Descripcion**
Inserta la entidad si no existe ningun registro que coincida con `matchCondition`; en caso contrario, actualiza el registro encontrado. Retorna la entidad insertada o actualizada.

**Ejemplo**
```csharp
var product = new Product { Sku = "SKU-001", Name = "Widget", Price = 9.99m };

Product resultado = await evaluator.UpsertAsync(
    product,
    matchCondition: p => p.Sku == product.Sku);
```

---

### `UpsertRangeAsync`

**Firma**
```csharp
Task<IEnumerable<T>> UpsertRangeAsync<TProperty>(
    IEnumerable<T> entities,
    Expression<Func<T, TProperty>> keySelector,
    bool saveChanges = true,
    CancellationToken cancellationToken = default
) where TProperty : notnull
```

**Descripcion**
Inserta o actualiza una coleccion de entidades usando el selector de clave para encontrar registros existentes. Retorna todas las entidades insertadas o actualizadas.

**Ejemplo**
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

**Firma**
```csharp
Task ExecuteTransactionAsync(Func<Task> operations, CancellationToken cancellationToken = default)
```

**Descripcion**
Ejecuta el delegado proporcionado dentro de una transaccion de base de datos. Si alguna operacion lanza una excepcion, la transaccion se revierte automaticamente.

**Ejemplo**
```csharp
await evaluator.ExecuteTransactionAsync(async () =>
{
    await orderEvaluator.AddAsync(nuevoPedido, saveChanges: false);
    await inventoryEvaluator.UpdateAsync(inventarioActualizado, saveChanges: false);
    await orderEvaluator.SaveChangesAsync();
});
```

> **Nota:** Lanza `InvalidOperationException` si la transaccion o su reversion falla.

---

### `ExecuteUpdateAsync`

**Firma**
```csharp
Task<int> ExecuteUpdateAsync(
    Expression<Func<T, bool>> condition,
    Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls,
    CancellationToken cancellationToken = default)
```

**Descripcion**
Emite un `UPDATE` SQL directo para todas las entidades que satisfacen `condition` sin cargarlas en memoria. Usa `ExecuteUpdateAsync` de EF Core 7+. Retorna el numero de filas afectadas.

**Ejemplo**
```csharp
int afectados = await evaluator.ExecuteUpdateAsync(
    condition: o => o.Status == OrderStatus.Pending && o.CreatedAt < DateTime.UtcNow.AddDays(-30),
    setPropertyCalls: s => s
        .SetProperty(o => o.Status, OrderStatus.Cancelled)
        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));
```

> **Nota:** No activa el seguimiento de cambios ni los eventos de ciclo de vida de entidad de EF Core.

---

### `BulkInsertAsync`

**Firma**
```csharp
Task BulkInsertAsync(IEnumerable<T> entities, BulkConfig? bulkConfig = null, CancellationToken cancellationToken = default)
```

**Descripcion**
Inserta una gran coleccion de entidades en una unica operacion masiva de alto rendimiento mediante `EFCore.BulkExtensions`. Significativamente mas rapido que `AddRangeAsync` + `SaveChangesAsync` para miles de registros.

Opciones de `BulkConfig` de interes:
- `BatchSize` — numero de filas por round-trip a la base de datos
- `SetOutputIdentity` — recupera las claves primarias autogeneradas tras la insercion
- `IncludeGraph` — incluye entidades relacionadas en la insercion

**Ejemplo**
```csharp
var products = Enumerable.Range(1, 10_000)
    .Select(i => new Product { Name = $"Producto {i}", Price = i * 1.5m })
    .ToList();

var config = new BulkConfig { BatchSize = 1000, SetOutputIdentity = true };
await evaluator.BulkInsertAsync(products, config);
```

---

### `BulkUpdateAsync`

**Firma**
```csharp
Task BulkUpdateAsync(IEnumerable<T> entities, BulkConfig? bulkConfig = null, CancellationToken cancellationToken = default)
```

**Descripcion**
Actualiza una gran coleccion de entidades en una unica operacion masiva. Las entidades deben tener claves primarias validas.

Opciones de `BulkConfig` de interes:
- `BatchSize` — filas por round-trip
- `PropertiesToInclude` — limita que columnas se actualizan

**Ejemplo**
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

**Firma**
```csharp
Task BulkDeleteAsync(IEnumerable<T> entities, BulkConfig? bulkConfig = null, CancellationToken cancellationToken = default)
```

**Descripcion**
Elimina una gran coleccion de entidades en una unica operacion masiva. Las entidades deben tener claves primarias validas.

**Ejemplo**
```csharp
var pedidosObsoletos = await (await evaluator.EvaluateQueryAsync(spec)).ToListAsync();

var config = new BulkConfig { BatchSize = 1000 };
await evaluator.BulkDeleteAsync(pedidosObsoletos, config);
```

---

### `BulkInsertOrUpdateAsync`

**Firma**
```csharp
Task BulkInsertOrUpdateAsync(IEnumerable<T> entities, BulkConfig? bulkConfig = null, CancellationToken cancellationToken = default)
```

**Descripcion**
Inserta entidades nuevas y actualiza las existentes en una unica operacion masiva (upsert masivo). La existencia se determina comparando claves primarias o las propiedades definidas en `PropertiesToIncludeOnCompare`.

Opciones de `BulkConfig` de interes:
- `BatchSize`
- `SetOutputIdentity` — recupera claves generadas para las nuevas inserciones
- `PropertiesToIncludeOnCompare` — define que columnas determinan si la entidad existe

**Ejemplo**
```csharp
var products = new List<Product>
{
    new Product { Id = 1, Name = "Producto Existente", Price = 15.99m },
    new Product { Name = "Producto Nuevo", Price = 7.99m },
};

var config = new BulkConfig
{
    BatchSize = 1000,
    SetOutputIdentity = true,
    PropertiesToIncludeOnCompare = new List<string> { nameof(Product.Id) }
};

await evaluator.BulkInsertOrUpdateAsync(products, config);
```
