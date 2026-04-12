# Vali-Flow.NoSql.Redis — Referencia Completa

## Tabla de Contenidos

1. [Descripción general](#descripción-general)
2. [Instalación](#instalación)
3. [Inicio rápido](#inicio-rápido)
4. [Métodos de extensión](#métodos-de-extensión)
5. [Operaciones soportadas](#operaciones-soportadas)
6. [Mapeo de tipos](#mapeo-de-tipos)
7. [Conversor de valores personalizado](#conversor-de-valores-personalizado)
8. [Limitaciones](#limitaciones)
9. [Ejemplo completo](#ejemplo-completo)

---

## Descripción general

**Vali-Flow.NoSql.Redis** traduce un árbol de expresiones `ValiFlow<T>` en una cadena de consulta RediSearch. La salida se pasa directamente a `db.FT().Search(indexName, new Query(result))`.

El paquete depende únicamente de `NRedisStack` para el tipo de consulta. Es un constructor de consultas puro sin preocupaciones de conexión o ejecución.

> NRedisStack 1.3.0 y posteriores agregan automáticamente `DIALECT 2` a los comandos de búsqueda. DIALECT 2 es necesario para valores de tag con comillas (usado por igualdad de cadenas y consultas IN). Si usas una versión anterior de NRedisStack, agrega `DIALECT 2` manualmente.

---

## Instalación

```bash
dotnet add package Vali-Flow.NoSql.Redis
```

`Vali-Flow.Core` se incluye como dependencia transitiva.

---

## Inicio rápido

```csharp
using NRedisStack;
using NRedisStack.Search;
using StackExchange.Redis;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Redis.Extensions;

var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Price, 50m);

string query = filter.ToRedisSearch();
// query → "(@IsActive:[1 1] @Price:[(50 +inf])"

IDatabase db = redis.GetDatabase();
SearchResult results = db.FT().Search("idx:products", new Query(query));
```

---

## Métodos de extensión

Ambas sobrecargas están en `Vali_Flow.NoSql.Redis.Extensions.ValiFlowRedisSearchExtensions`.

### `ToRedisSearch<T>(this ValiFlow<T> flow, Func<object?, string?>? customConverter = null)`

Traduce las condiciones acumuladas en un constructor `ValiFlow<T>` en una cadena de consulta RediSearch.

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `flow` | `ValiFlow<T>` | Sí | El constructor que contiene las condiciones. |
| `customConverter` | `Func<object?, string?>?` | No | Hook para mapear tipos CLR a su cadena de valor tag. Retornar `null` para caer al conversor por defecto. |

**Retorna:** `string` — pásalo a `new Query(result)` o úsalo directamente en `FT().Search(...)`.

### `ToRedisSearch<T>(this Expression<Func<T, bool>> expression, Func<object?, string?>? customConverter = null)`

Misma traducción para una `Expression<Func<T, bool>>` ya construida.

```csharp
Expression<Func<Order, bool>> expr = o => o.Status == "Pending";
string query = expr.ToRedisSearch();
```

---

## Operaciones soportadas

| Método ValiFlow | Nodo IR | Salida RediSearch |
|----------------|---------|-------------------|
| `.EqualTo(x => x.NumField, 42)` | `EqualNode` (numérico) | `@NumField:[42 42]` |
| `.EqualTo(x => x.StrField, "v")` | `EqualNode` (cadena) | `@StrField:{"v"}` |
| `.NotEqualTo(x => x.NumField, 42)` | `EqualNode(IsNegated, numérico)` | `(-@NumField:[42 42])` |
| `.NotEqualTo(x => x.StrField, "v")` | `EqualNode(IsNegated, cadena)` | `-@StrField:{"v"}` |
| `.GreaterThan(x => x.Field, v)` | `ComparisonNode(GT)` | `@Field:[(v +inf]` |
| `.GreaterThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(GTE)` | `@Field:[v +inf]` |
| `.LessThan(x => x.Field, v)` | `ComparisonNode(LT)` | `@Field:[-inf (v]` |
| `.LessThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(LTE)` | `@Field:[-inf v]` |
| `.Contains(x => x.Field, "txt")` | `LikeNode(Contains)` | `@Field:*txt*` |
| `.StartsWith(x => x.Field, "pre")` | `LikeNode(StartsWith)` | `@Field:pre*` |
| `.EndsWith(x => x.Field, "suf")` | `LikeNode(EndsWith)` | `@Field:*suf` |
| `.In(x => x.Field, [1, 2, 3])` | `InNode` (numérico) | `(@Field:[1 1]\|@Field:[2 2]\|@Field:[3 3])` |
| `.In(x => x.Field, ["a", "b"])` | `InNode` (cadena) | `@Field:{"a"\|"b"}` |
| `.And(a, b)` | `AndNode` | `(a b)` |
| `.Or(a, b)` | `OrNode` | `(a \| b)` |
| `.Not(inner)` | `NotNode` | `-(inner)` |

**Límites de rango exclusivos:** RediSearch usa `(value` (paréntesis de apertura antes del número) para expresar desigualdad estricta. `GreaterThan` emite `[(v +inf]` y `LessThan` emite `[-inf (v]`.

**Escape de tags:** `\` y `"` dentro de valores de tag con comillas se escapan a `\\` y `\"`.

**Lista IN mixta:** Si un `InNode` contiene una mezcla de valores numéricos y no numéricos, se lanza `InvalidOperationException`. Todos los valores no nulos en una llamada `In` deben ser del mismo tipo (todos numérico/bool, o todos cadenas).

---

## Mapeo de tipos

Los valores se clasifican como numéricos (mapeados a consultas de rango) o cadena (mapeados a consultas de tag).

**Tipos numérico/bool** (usan sintaxis de consulta de rango `@field:[v v]`):

| Tipo CLR | Representación RediSearch |
|----------|--------------------------|
| `bool` | `1` (true) o `0` (false) |
| `int` | representación en cadena |
| `long` | representación en cadena |
| `double` | representación en cadena (InvariantCulture) |
| `float` | representación en cadena (InvariantCulture) |
| `decimal` | convertido a `double`, luego cadena |

**Tipos cadena/otros** (usan sintaxis de consulta de tag `@field:{"value"}`):

| Tipo CLR | Valor tag |
|----------|-----------|
| `string` | la cadena misma |
| `Enum` | `.ToString()` (nombre, no número) |
| cualquier otro | `.ToString()` |

---

## Conversor de valores personalizado

Usa `customConverter` para controlar cómo aparecen los tipos de dominio en la cadena de consulta. El conversor se llama **antes** de la clasificación incorporada. Retorna `null` para dejar que el default maneje el valor.

```csharp
// Tipo de dominio
record Money(decimal Amount, string Currency);

// Conversor: usar el monto numérico para que aterrice en una consulta de rango
string query = new ValiFlow<Product>()
    .GreaterThan(x => x.Price, new Money(100m, "USD"))
    .ToRedisSearch(value =>
    {
        if (value is Money m)
            return m.Amount.ToString(CultureInfo.InvariantCulture);
        return null;
    });

// query → "@Price:[(100 +inf]"
```

Retornar una cadena desde el conversor siempre produce una consulta de tag para igualdad, y se usa tal cual para consultas de rango (así que retorna una cadena numérica en cultura invariante para campos numéricos).

---

## Limitaciones

| Limitación | Detalle |
|-----------|---------|
| `IsNull` / `IsNotNull` | Lanza `NotSupportedException`. RediSearch no tiene consulta nativa de existencia de campo. Maneja las verificaciones de nulos a nivel de aplicación o usa un valor centinela en tu índice. |
| Lista `In` vacía | Produce `(-*)` — una negación del token de coincidencia total, que siempre es falso. |
| `In` numérico/cadena mixto | Lanza `InvalidOperationException`. Todos los valores no nulos en una sola llamada `In` deben ser del mismo tipo. |
| Campos de patrón | `Contains`, `StartsWith` y `EndsWith` apuntan a campos TEXT. El campo debe estar indexado como `TEXT` en tu schema RediSearch para que la búsqueda con prefijo/sufijo funcione correctamente. |
| DIALECT 2 | Los valores de tag con comillas requieren DIALECT 2. NRedisStack 1.3.0+ lo agrega automáticamente. Las versiones anteriores requieren `new Query(query).Dialect(2)`. |

---

## Ejemplo completo

Un filtro realista sobre datos de producto, combinado con la ejecución de NRedisStack:

```csharp
using NRedisStack;
using NRedisStack.Search;
using StackExchange.Redis;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Redis.Extensions;

// Construir el filtro
var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .In(x => x.Category, new[] { "Electronics", "Computers" })
    .GreaterThanOrEqualTo(x => x.Price, 50m)
    .LessThan(x => x.Price, 1500m)
    .StartsWith(x => x.Name, "Pro");

string queryString = filter.ToRedisSearch();
// queryString:
// "(@IsActive:[1 1] (@Category:{"Electronics"|"Computers"} (@Price:[50 +inf] (@Price:[-inf (1500] @Name:Pro*))))"

// Ejecutar
ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
IDatabase db = redis.GetDatabase();

SearchResult result = db.FT().Search("idx:products", new Query(queryString)
    .ReturnFields("Name", "Price", "Category")
    .Limit(0, 25));

foreach (Document doc in result.Documents)
{
    Console.WriteLine($"{doc["Name"]} — {doc["Price"]}");
}
```

### Conversor personalizado para un value object

```csharp
// Precio almacenado en centavos (entero) en Redis
record Price(int Cents);

string query = new ValiFlow<Product>()
    .GreaterThanOrEqualTo(x => x.UnitPrice, new Price(1000))
    .ToRedisSearch(value =>
    {
        if (value is Price p)
            return p.Cents.ToString(CultureInfo.InvariantCulture);
        return null;
    });

// query → "@UnitPrice:[1000 +inf]"
```
