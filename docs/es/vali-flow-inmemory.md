# Vali-Flow.InMemory

## Descripcion general

**Vali-Flow.InMemory** es un evaluador sincronico en memoria, sin dependencias externas, para el ecosistema Vali-Flow. Opera sobre colecciones `IEnumerable<T>` en lugar de un `DbContext`, lo que lo hace ideal para:

- **Pruebas unitarias** — inyectar un evaluador pre-cargado en reemplazo del evaluador EF Core real, sin necesidad de base de datos.
- **Capas de cache** — evaluar reglas de negocio contra un cache en memoria sin consultar la base de datos.
- **Prototipado** — explorar la API de Vali-Flow sin ningun tipo de infraestructura.

### Diferencias respecto al evaluador de EF Core

| Aspecto | `Vali-Flow` (EF Core) | `Vali-Flow.InMemory` |
|---------|----------------------|----------------------|
| Fuente de datos | `DbContext` / base de datos SQL | `IEnumerable<T>` |
| Ejecucion | Asincronica (`async/await`) | **Sincronica** |
| Traduccion de consultas | LINQ-to-SQL via EF Core | LINQ-to-Objects |
| Includes / navegacion | `IEfInclude<T>` (JOINs) | No aplica |
| Seguimiento de cambios | Change tracker de EF Core | Listas internas rastreadas |

### Instalacion

```bash
dotnet add package Vali-Flow.InMemory
```

---

## Configuracion inicial

### Constructor

```csharp
public ValiFlowEvaluator<T, TProperty>(
    IEnumerable<T>? initialData = null,
    ValiFlow<T>? valiFlow = null,
    Func<T, TProperty>? getId = null)
```

**Parametros de tipo**

| Parametro | Descripcion |
|-----------|-------------|
| `T` | Tipo de entidad (debe ser un tipo por referencia). |
| `TProperty` | Tipo de la clave primaria (por ej., `int`, `Guid`, `string`). |

**Parametros del constructor**

| Parametro | Tipo | Descripcion |
|-----------|------|-------------|
| `initialData` | `IEnumerable<T>?` | Datos iniciales copiados al almacen interno. Pasar `null` para comenzar con un almacen vacio. |
| `valiFlow` | `ValiFlow<T>?` | Filtro por defecto aplicado por todos los metodos cuando no se provee un filtro por llamada. Pasar `null` para coincidir con todas las entidades. |
| `getId` | `Func<T, TProperty>?` | Extractor de clave usado por las operaciones de escritura para identificar entidades. Si es `null`, el constructor busca una propiedad publica llamada `Id` via reflexion (cacheada una sola vez en la construccion). Lanza `InvalidOperationException` si no se encuentra `Id`. |

**Ejemplo**

```csharp
var productos = new List<Producto>
{
    new() { Id = 1, Nombre = "Widget",     Precio = 10m,  Activo = true  },
    new() { Id = 2, Nombre = "Gadget",     Precio = 120m, Activo = true  },
    new() { Id = 3, Nombre = "Doohickey",  Precio = 5m,   Activo = false },
};

var filtro = new ValiFlow<Producto>().IsTrue(p => p.Activo);

// Extractor de clave explicito
var evaluador = new ValiFlowEvaluator<Producto, int>(
    initialData: productos,
    valiFlow: filtro,
    getId: p => p.Id);

// Deteccion automatica de la propiedad 'Id'
var evaluadorAuto = new ValiFlowEvaluator<Producto, int>(productos, filtro);
```

---

## El parametro `negateCondition`

Todos los metodos de lectura aceptan un parametro `bool negateCondition` (por defecto `false`). Cuando se establece en `true`, el filtro ValiFlow se invierte logicamente usando `BuildNegated()` internamente — equivalente a envolver toda la expresion en un `!`.

Esto es util cuando se desea consultar elementos que **no cumplen** una regla de validacion sin necesidad de definir un filtro separado.

### Ejemplo antes/despues

```csharp
var filtro = new ValiFlow<Producto>().GreaterThan(p => p.Precio, 50m);

var evaluador = new ValiFlowEvaluator<Producto, int>(productos, filtro, p => p.Id);

// Normal: productos con Precio > 50
IEnumerable<Producto> caros = evaluador.EvaluateAll<int>(null);

// Negado: productos con Precio <= 50  (mismo filtro, invertido)
IEnumerable<Producto> baratos = evaluador.EvaluateAll<int>(null, negateCondition: true);
```

> **Nota:** Las condiciones negadas derivadas del `_valiFlow` a nivel de instancia se compilan una vez y se cachean. Pasar un `valiFlow` explicito por llamada omite el cache y compila en cada invocacion.

### Tabla de comportamiento

| Familia de metodo | `negateCondition = false` | `negateCondition = true` |
|-------------------|--------------------------|--------------------------|
| `EvaluateAll` | Entidades que cumplen el filtro | Entidades que **no** cumplen el filtro |
| `EvaluateAllFailed` | Entidades que **no** cumplen el filtro | Entidades que cumplen el filtro |
| `GetFirst` / `GetLast` | Primera/ultima entidad que cumple | Primera/ultima entidad que **no** cumple |
| `GetFirstFailed` / `GetLastFailed` | Primera/ultima que **no** cumple | Primera/ultima que cumple |
| Agregados / agrupacion | Calculados sobre entidades que cumplen | Calculados sobre entidades que **no** cumplen |

---

## Metodos de lectura

### `Evaluate`

**Firma**
```csharp
bool Evaluate(T entity, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Descripcion**
Evalua una sola entidad contra el filtro. Retorna `true` si la entidad cumple la condicion; `false` en caso contrario.

**Ejemplo**
```csharp
var producto = new Producto { Id = 1, Precio = 80m, Activo = true };
var filtro = new ValiFlow<Producto>().GreaterThan(p => p.Precio, 50m);
var evaluador = new ValiFlowEvaluator<Producto, int>(getId: p => p.Id);

bool cumple = evaluador.Evaluate(producto, filtro);                        // true
bool noCumple = evaluador.Evaluate(producto, filtro, negateCondition: true); // false
```

---

### `EvaluateAny`

**Firma**
```csharp
bool EvaluateAny(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Descripcion**
Retorna `true` si al menos una entidad de la coleccion cumple el filtro. Pasar `null` en `entities` para operar sobre el almacen interno.

**Ejemplo**
```csharp
bool hayCaros = evaluador.EvaluateAny(null);
// true si algun producto en el almacen tiene Precio > 50
```

---

### `EvaluateCount`

**Firma**
```csharp
int EvaluateCount(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Descripcion**
Cuenta la cantidad de entidades que cumplen el filtro.

**Ejemplo**
```csharp
int activosCount = evaluador.EvaluateCount(null);
// retorna 2 (Widget y Gadget estan activos)

int inactivosCount = evaluador.EvaluateCount(null, negateCondition: true);
// retorna 1 (Doohickey esta inactivo)
```

---

### `GetFirst`

**Firma**
```csharp
T? GetFirst(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Descripcion**
Retorna la primera entidad que cumple el filtro, o `null` si ninguna coincide. No se aplica ordenamiento; el resultado depende del orden de enumeracion.

**Ejemplo**
```csharp
Producto? primero = evaluador.GetFirst(null);
// Retorna el primer producto activo en el orden del almacen
```

---

### `GetFirstFailed`

**Firma**
```csharp
T? GetFirstFailed(IEnumerable<T>? entities, ValiFlow<T>? valiFlow = null, bool negateCondition = false)
```

**Descripcion**
Retorna la primera entidad que **no** cumple el filtro. Cuando `negateCondition = true`, la logica se invierte y retorna la primera que si cumple (equivalente a `GetFirst`).

**Ejemplo**
```csharp
Producto? primerInactivo = evaluador.GetFirstFailed(null);
// Retorna Doohickey (Activo = false)
```

---

### `EvaluateAll`

**Firma**
```csharp
IEnumerable<T> EvaluateAll<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Descripcion**
Retorna todas las entidades que cumplen el filtro, con ordenamiento primario y secundario opcionales.

**Ejemplo**
```csharp
// Todos los productos activos ordenados por precio descendente
IEnumerable<Producto> ordenados = evaluador.EvaluateAll<decimal>(
    entities: null,
    orderBy: p => p.Precio,
    ascending: false);
```

---

### `EvaluateAllFailed`

**Firma**
```csharp
IEnumerable<T> EvaluateAllFailed<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Descripcion**
Retorna todas las entidades que **no** cumplen el filtro, con ordenamiento opcional. Util para reportes de validacion: recopilar todo lo que fallo una regla.

**Ejemplo**
```csharp
IEnumerable<Producto> inactivos = evaluador.EvaluateAllFailed<string>(
    entities: null,
    orderBy: p => p.Nombre);
```

---

### `GetLast`

**Firma**
```csharp
T? GetLast<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Descripcion**
Retorna la ultima entidad que cumple el filtro despues del ordenamiento opcional. Retorna `null` si ninguna coincide.

**Ejemplo**
```csharp
Producto? maCaro = evaluador.GetLast<decimal>(
    entities: null,
    orderBy: p => p.Precio,
    ascending: true);
```

---

### `GetLastFailed`

**Firma**
```csharp
T? GetLastFailed<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Descripcion**
Retorna la ultima entidad que **no** cumple el filtro despues del ordenamiento opcional.

**Ejemplo**
```csharp
Producto? ultimoInactivo = evaluador.GetLastFailed<string>(
    entities: null,
    orderBy: p => p.Nombre);
```

---

### `EvaluatePaged`

**Firma**
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

**Descripcion**
Retorna un segmento paginado de las entidades filtradas. `page` es base 1.

> **Nota:** Tanto `page` como `pageSize` deben ser >= 1; de lo contrario, se lanza una `ArgumentOutOfRangeException`.

**Ejemplo**
```csharp
// Pagina 2, 10 elementos por pagina, productos activos ordenados por nombre
IEnumerable<Producto> pagina2 = evaluador.EvaluatePaged<string>(
    entities: todosLosProductos,
    page: 2,
    pageSize: 10,
    orderBy: p => p.Nombre);
```

---

### `EvaluatePagedResult`

**Firma**
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

**Descripcion**
Igual que `EvaluatePaged`, pero retorna un `PagedResult<T>` que incluye metadatos de paginacion.

**Propiedades de `PagedResult<T>`**

| Propiedad | Tipo | Descripcion |
|-----------|------|-------------|
| `Items` | `IReadOnlyList<T>` | Elementos de la pagina actual. |
| `TotalCount` | `int` | Total de entidades coincidentes en todas las paginas. |
| `Page` | `int` | Numero de pagina actual (base 1). |
| `PageSize` | `int` | Maximo de elementos por pagina. |
| `TotalPages` | `int` | Numero total de paginas. |
| `HasNextPage` | `bool` | Indica si existe una pagina siguiente. |
| `HasPreviousPage` | `bool` | Indica si existe una pagina anterior. |

**Ejemplo**
```csharp
PagedResult<Producto> resultado = evaluador.EvaluatePagedResult<decimal>(
    entities: null,
    page: 1,
    pageSize: 5,
    orderBy: p => p.Precio,
    ascending: false);

Console.WriteLine($"Pagina {resultado.Page} de {resultado.TotalPages} — {resultado.TotalCount} en total");
```

---

### `EvaluateTop`

**Firma**
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

**Descripcion**
Retorna los N primeros elementos que cumplen el filtro. `count` debe ser > 0.

**Ejemplo**
```csharp
// Los 3 productos activos mas caros
IEnumerable<Producto> top3 = evaluador.EvaluateTop<decimal>(
    entities: null,
    count: 3,
    orderBy: p => p.Precio,
    ascending: false);
```

---

### `EvaluateDistinct`

**Firma**
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

**Descripcion**
Retorna una entidad representativa por cada valor de clave distinto, conservando solo la primera ocurrencia dentro de cada grupo despues de aplicar el filtro.

**Ejemplo**
```csharp
// Un producto por categoria
IEnumerable<Producto> unoPorCategoria = evaluador.EvaluateDistinct<string>(
    entities: null,
    selector: p => p.Categoria);
```

---

### `EvaluateDuplicates`

**Firma**
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

**Descripcion**
Retorna todas las entidades cuyo valor de clave aparece mas de una vez en el conjunto filtrado.

**Ejemplo**
```csharp
// Productos con SKU duplicado entre los activos
IEnumerable<Producto> skusDuplicados = evaluador.EvaluateDuplicates<string>(
    entities: null,
    selector: p => p.Sku);
```

---

### `GetFirstMatchIndex`

**Firma**
```csharp
int GetFirstMatchIndex<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Descripcion**
Retorna el indice de base cero de la primera entidad que coincide despues del ordenamiento. Retorna `-1` si ninguna entidad coincide.

**Ejemplo**
```csharp
int idx = evaluador.GetFirstMatchIndex<decimal>(
    entities: null,
    orderBy: p => p.Precio);
// Indice del producto activo mas barato en la lista ordenada
```

---

### `GetLastMatchIndex`

**Firma**
```csharp
int GetLastMatchIndex<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey>? orderBy = null,
    bool ascending = true,
    IEnumerable<InMemoryThenBy<T, TKey>>? thenBys = null,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
```

**Descripcion**
Retorna el indice de base cero de la ultima entidad que coincide despues del ordenamiento. Retorna `-1` si ninguna entidad coincide.

**Ejemplo**
```csharp
int idx = evaluador.GetLastMatchIndex<decimal>(
    entities: null,
    orderBy: p => p.Precio);
// Indice del producto activo mas caro en la lista ordenada
```

---

### `EvaluateMin`

**Firma**
```csharp
TResult EvaluateMin<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Descripcion**
Calcula el valor minimo de una propiedad numerica entre las entidades filtradas. Retorna `TResult.Zero` si el conjunto filtrado esta vacio.

**Ejemplo**
```csharp
decimal precioMin = evaluador.EvaluateMin<decimal>(null, p => p.Precio);
```

---

### `EvaluateMax`

**Firma**
```csharp
TResult EvaluateMax<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Descripcion**
Calcula el valor maximo de una propiedad numerica entre las entidades filtradas. Retorna `TResult.Zero` si el conjunto filtrado esta vacio.

**Ejemplo**
```csharp
decimal precioMax = evaluador.EvaluateMax<decimal>(null, p => p.Precio);
```

---

### `EvaluateAverage`

**Firma**
```csharp
decimal EvaluateAverage<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Descripcion**
Calcula el promedio de una propiedad numerica entre las entidades filtradas. Retorna `0` si el conjunto filtrado esta vacio. Siempre retorna `decimal`.

**Ejemplo**
```csharp
decimal precioPromedio = evaluador.EvaluateAverage<decimal>(null, p => p.Precio);
```

---

### `EvaluateSum`

**Firma**
```csharp
TResult EvaluateSum<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Descripcion**
Calcula la suma de una propiedad numerica entre las entidades filtradas.

**Ejemplo**
```csharp
decimal ingresoTotal = evaluador.EvaluateSum<decimal>(null, p => p.Precio);
```

---

### `EvaluateAggregate`

**Firma**
```csharp
TResult EvaluateAggregate<TResult>(
    IEnumerable<T>? entities,
    Func<T, TResult> selector,
    Func<TResult, TResult, TResult> aggregator,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult>
```

**Descripcion**
Aplica una funcion acumuladora personalizada sobre la propiedad numerica seleccionada de las entidades filtradas. El valor inicial del acumulador es `TResult.Zero`.

**Ejemplo**
```csharp
// Suma personalizada ponderada de cantidades en stock
int resultado = evaluador.EvaluateAggregate<int>(
    entities: null,
    selector: p => p.Stock,
    aggregator: (acc, val) => acc + val * 2);
```

---

### `EvaluateGrouped`

**Firma**
```csharp
Dictionary<TKey, List<T>> EvaluateGrouped<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna un diccionario de clave → lista de entidades.

**Ejemplo**
```csharp
Dictionary<string, List<Producto>> porCategoria =
    evaluador.EvaluateGrouped<string>(null, p => p.Categoria);
```

---

### `EvaluateCountByGroup`

**Firma**
```csharp
Dictionary<TKey, int> EvaluateCountByGroup<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna un diccionario de clave → conteo.

**Ejemplo**
```csharp
Dictionary<string, int> cantidadPorCategoria =
    evaluador.EvaluateCountByGroup<string>(null, p => p.Categoria);
```

---

### `EvaluateSumByGroup`

**Firma**
```csharp
Dictionary<TKey, TResult> EvaluateSumByGroup<TKey, TResult>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult> where TKey : notnull
```

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna la suma de una propiedad numerica por grupo.

**Ejemplo**
```csharp
Dictionary<string, decimal> ingresoPorCategoria =
    evaluador.EvaluateSumByGroup<string, decimal>(null, p => p.Categoria, p => p.Precio);
```

---

### `EvaluateMinByGroup`

**Firma**
```csharp
Dictionary<TKey, TResult> EvaluateMinByGroup<TKey, TResult>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult> where TKey : notnull
```

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna el valor minimo de una propiedad numerica por grupo.

**Ejemplo**
```csharp
Dictionary<string, decimal> precioMinPorCategoria =
    evaluador.EvaluateMinByGroup<string, decimal>(null, p => p.Categoria, p => p.Precio);
```

---

### `EvaluateMaxByGroup`

**Firma**
```csharp
Dictionary<TKey, TResult> EvaluateMaxByGroup<TKey, TResult>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult> where TKey : notnull
```

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna el valor maximo de una propiedad numerica por grupo.

**Ejemplo**
```csharp
Dictionary<string, decimal> precioMaxPorCategoria =
    evaluador.EvaluateMaxByGroup<string, decimal>(null, p => p.Categoria, p => p.Precio);
```

---

### `EvaluateAverageByGroup`

**Firma**
```csharp
Dictionary<TKey, decimal> EvaluateAverageByGroup<TKey, TResult>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    Func<T, TResult> selector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TResult : INumber<TResult> where TKey : notnull
```

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna el promedio de una propiedad numerica por grupo como `decimal`.

**Ejemplo**
```csharp
Dictionary<string, decimal> precioPromedioPorCategoria =
    evaluador.EvaluateAverageByGroup<string, decimal>(null, p => p.Categoria, p => p.Precio);
```

---

### `EvaluateDuplicatesByGroup`

**Firma**
```csharp
Dictionary<TKey, List<T>> EvaluateDuplicatesByGroup<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna solo los grupos que contienen mas de una entidad (grupos duplicados).

**Ejemplo**
```csharp
// Categorias con mas de un producto activo
Dictionary<string, List<Producto>> duplicados =
    evaluador.EvaluateDuplicatesByGroup<string>(null, p => p.Categoria);
```

---

### `EvaluateUniquesByGroup`

**Firma**
```csharp
Dictionary<TKey, T> EvaluateUniquesByGroup<TKey>(
    IEnumerable<T>? entities,
    Func<T, TKey> keySelector,
    ValiFlow<T>? valiFlow = null,
    bool negateCondition = false)
    where TKey : notnull
```

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna solo los grupos con exactamente una entidad (grupos unicos). El valor del diccionario es la entidad unica, no una lista.

**Ejemplo**
```csharp
// Categorias con exactamente un producto activo
Dictionary<string, Producto> unicos =
    evaluador.EvaluateUniquesByGroup<string>(null, p => p.Categoria);
```

---

### `EvaluateTopByGroup`

**Firma**
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

**Descripcion**
Agrupa las entidades filtradas por una clave y retorna los N primeros elementos por grupo, con ordenamiento intra-grupo opcional.

**Ejemplo**
```csharp
// Los 2 productos mas caros por categoria
Dictionary<string, List<Producto>> top2PorCategoria =
    evaluador.EvaluateTopByGroup<string, decimal>(
        entities: null,
        keySelector: p => p.Categoria,
        count: 2,
        orderBy: p => p.Precio,
        ascending: false);
```

---

## Metodos de escritura

Todos los metodos de escritura operan sobre el almacen interno por defecto. Pasar una coleccion `entities` explicita para operar sobre una lista externa.

### `Add`

**Firma**
```csharp
bool Add(T entity, IEnumerable<T>? entities = null)
```

**Descripcion**
Agrega una sola entidad al almacen. Retorna `true` si la operacion fue exitosa.

**Ejemplo**
```csharp
var nuevoProducto = new Producto { Id = 4, Nombre = "Thingamajig", Precio = 30m, Activo = true };
bool agregado = evaluador.Add(nuevoProducto);
```

---

### `AddRange`

**Firma**
```csharp
void AddRange(IEnumerable<T> entitiesToAdd, IEnumerable<T>? entities = null)
```

**Descripcion**
Agrega multiples entidades al almacen en una sola llamada.

**Ejemplo**
```csharp
evaluador.AddRange(new[]
{
    new Producto { Id = 5, Nombre = "Alpha", Precio = 15m, Activo = true },
    new Producto { Id = 6, Nombre = "Beta",  Precio = 25m, Activo = true },
});
```

---

### `Update`

**Firma**
```csharp
T? Update(T entity, IEnumerable<T>? entities = null)
```

**Descripcion**
Reemplaza la entidad almacenada con la misma clave que la entidad provista. Retorna la entidad actualizada, o `null` si no se encontro ninguna entidad coincidente.

**Ejemplo**
```csharp
var modificado = new Producto { Id = 1, Nombre = "Widget Pro", Precio = 15m, Activo = true };
Producto? resultado = evaluador.Update(modificado);
```

---

### `UpdateRange`

**Firma**
```csharp
IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate, IEnumerable<T>? entities = null)
```

**Descripcion**
Actualiza multiples entidades. Retorna la coleccion de entidades actualizadas exitosamente.

**Ejemplo**
```csharp
IEnumerable<Producto> actualizados = evaluador.UpdateRange(productosModificados);
```

---

### `Delete`

**Firma**
```csharp
bool Delete(T entity, IEnumerable<T>? entities = null)
```

**Descripcion**
Elimina una sola entidad del almacen haciendo coincidir su clave. Retorna `true` si fue encontrada y eliminada.

**Ejemplo**
```csharp
bool eliminado = evaluador.Delete(producto);
```

---

### `DeleteRange`

**Firma**
```csharp
int DeleteRange(IEnumerable<T> entitiesToDelete, IEnumerable<T>? entities = null)
```

**Descripcion**
Elimina multiples entidades del almacen. Retorna la cantidad de entidades eliminadas exitosamente.

**Ejemplo**
```csharp
int cantidad = evaluador.DeleteRange(productosObsoletos);
```

---

### `DeleteByCondition`

**Firma**
```csharp
int DeleteByCondition(Func<T, bool> predicate, IEnumerable<T>? entities = null)
```

**Descripcion**
Elimina todas las entidades que satisfacen un predicado. Retorna la cantidad de entidades eliminadas.

**Ejemplo**
```csharp
// Eliminar todos los productos inactivos
int eliminados = evaluador.DeleteByCondition(p => !p.Activo);
```

---

### `Upsert`

**Firma**
```csharp
T Upsert(T entity, IEnumerable<T>? entities = null)
```

**Descripcion**
Inserta la entidad si su clave no existe en el almacen; la actualiza si la clave ya existe. Retorna la entidad despues de la operacion.

**Ejemplo**
```csharp
var orden = new Orden { Id = 99, ClienteId = 1, Total = 250m };
Orden upserted = evaluador.Upsert(orden);
```

---

### `UpsertRange`

**Firma**
```csharp
IEnumerable<T> UpsertRange(IEnumerable<T> entitiesToUpsert, IEnumerable<T>? entities = null)
```

**Descripcion**
Realiza `Upsert` sobre cada entidad de la coleccion. Retorna todas las entidades procesadas.

**Ejemplo**
```csharp
IEnumerable<Orden> resultados = evaluador.UpsertRange(ordenesEntrantes);
```

---

### `SaveChanges`

**Firma**
```csharp
void SaveChanges(IEnumerable<T>? entities = null)
```

**Descripcion**
Aplica cualquier cambio pendiente rastreado (adiciones, actualizaciones, eliminaciones) al almacen. Llamar a este metodo despues de agrupar operaciones de escritura si se difirieron.

**Ejemplo**
```csharp
evaluador.Add(productoA);
evaluador.Add(productoB);
evaluador.SaveChanges();
```

---

## `SetValiFlow`

**Firma**
```csharp
void SetValiFlow(ValiFlow<T> valiFlow)
```

**Descripcion**
Reemplaza el filtro a nivel de instancia en tiempo de ejecucion. Limpia la condicion negada cacheada para que sea recompilada en la proxima llamada negada. Lanza `ArgumentNullException` si `valiFlow` es `null`.

Util cuando el evaluador se comparte entre casos de prueba y cada caso requiere un filtro diferente.

**Ejemplo**
```csharp
var evaluador = new ValiFlowEvaluator<Usuario, int>(usuarios, getId: u => u.Id);

// Sin filtro inicial: todos los usuarios
int totalCount = evaluador.EvaluateCount(null);

// Cambiar al filtro: solo usuarios premium
evaluador.SetValiFlow(new ValiFlow<Usuario>().IsTrue(u => u.EsPremium));
int premiumCount = evaluador.EvaluateCount(null);
```
