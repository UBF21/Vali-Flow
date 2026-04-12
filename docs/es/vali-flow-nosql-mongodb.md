# Vali-Flow.NoSql.MongoDB — Referencia Completa

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

**Vali-Flow.NoSql.MongoDB** traduce un árbol de expresiones `ValiFlow<T>` en un filtro MongoDB `BsonDocument`. La salida se acepta nativamente en cualquier lugar donde MongoDB espera un filtro — `FilterDefinition<T>` tiene una conversión implícita desde `BsonDocument`, por lo que no se necesita un cast explícito.

El paquete depende únicamente de `MongoDB.Bson`, no del driver completo `MongoDB.Driver`. Es un constructor de consultas puro sin preocupaciones de conexión o ejecución.

---

## Instalación

```bash
dotnet add package Vali-Flow.NoSql.MongoDB
```

`Vali-Flow.Core` se incluye como dependencia transitiva.

---

## Inicio rápido

```csharp
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.MongoDB.Extensions;

var filter = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Age, 18);

BsonDocument mongoFilter = filter.ToMongo();
var users = await collection.Find(mongoFilter).ToListAsync();
```

---

## Métodos de extensión

Ambas sobrecargas están en `Vali_Flow.NoSql.MongoDB.Extensions.ValiFlowMongoExtensions`.

### `ToMongo<T>(this ValiFlow<T> flow, Func<object?, BsonValue?>? customConverter = null)`

Traduce las condiciones acumuladas en un constructor `ValiFlow<T>` en un filtro `BsonDocument`.

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `flow` | `ValiFlow<T>` | Sí | El constructor que contiene las condiciones. |
| `customConverter` | `Func<object?, BsonValue?>?` | No | Hook para mapear tipos CLR no manejados por el switch incorporado. Retornar `null` para caer al conversor por defecto. |

**Retorna:** `BsonDocument` — pásalo directamente a `Find`, `CountDocuments`, `DeleteMany`, etc.

### `ToMongo<T>(this Expression<Func<T, bool>> expression, Func<object?, BsonValue?>? customConverter = null)`

Misma traducción para una `Expression<Func<T, bool>>` ya construida.

```csharp
Expression<Func<Order, bool>> expr = o => o.Status == "Pending" && o.Total > 50m;
BsonDocument filter = expr.ToMongo();
```

---

## Operaciones soportadas

| Método ValiFlow | Nodo IR | Salida MongoDB |
|----------------|---------|----------------|
| `.EqualTo(x => x.Field, v)` | `EqualNode(IsNegated: false)` | `{ field: value }` |
| `.NotEqualTo(x => x.Field, v)` | `EqualNode(IsNegated: true)` | `{ field: { $ne: value } }` |
| `.GreaterThan(x => x.Field, v)` | `ComparisonNode(GT)` | `{ field: { $gt: value } }` |
| `.GreaterThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(GTE)` | `{ field: { $gte: value } }` |
| `.LessThan(x => x.Field, v)` | `ComparisonNode(LT)` | `{ field: { $lt: value } }` |
| `.LessThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(LTE)` | `{ field: { $lte: value } }` |
| `.Contains(x => x.Field, "txt")` | `LikeNode(Contains)` | `{ field: { $regex: /txt/i } }` |
| `.StartsWith(x => x.Field, "pre")` | `LikeNode(StartsWith)` | `{ field: { $regex: /^pre/i } }` |
| `.EndsWith(x => x.Field, "suf")` | `LikeNode(EndsWith)` | `{ field: { $regex: /suf$/i } }` |
| `.In(x => x.Field, list)` | `InNode` | `{ field: { $in: [...] } }` |
| `.IsNull(x => x.Field)` | `NullNode(IsNull)` | `{ field: null }` |
| `.IsNotNull(x => x.Field)` | `NullNode(IsNotNull)` | `{ field: { $ne: null } }` |
| `.And(a, b)` | `AndNode` | `{ $and: [a, b] }` |
| `.Or(a, b)` | `OrNode` | `{ $or: [a, b] }` |
| `.Not(inner)` | `NotNode` | `{ $nor: [inner] }` |

Todas las coincidencias de patrón de cadena usan el flag de regex `i` (insensible a mayúsculas/minúsculas). Los caracteres especiales de regex en el patrón se escapan antes de insertarse.

---

## Mapeo de tipos

El switch `ToBsonValue` incorporado maneja estos tipos CLR:

| Tipo CLR | `BsonValue` producido |
|----------|----------------------|
| `null` | `BsonNull.Value` |
| `bool` | `BsonBoolean` |
| `int` | `BsonInt32` |
| `long` | `BsonInt64` |
| `double` | `BsonDouble` |
| `float` | `BsonDouble` |
| `decimal` | `BsonDecimal128` |
| `string` | `BsonString` |
| `DateTime` | `BsonDateTime` (UTC) |
| `DateTimeOffset` | `BsonDateTime` (.UtcDateTime) |
| `Guid` | `BsonBinaryData` (GuidRepresentation.Standard) |
| `Enum` | `BsonInt32` (valor entero subyacente) |
| cualquier otro | `BsonValue.Create(v)` |

---

## Conversor de valores personalizado

Usa `customConverter` cuando tus tipos de dominio no están en la tabla anterior, o cuando necesitas serialización diferente a la por defecto (por ejemplo, almacenar un `decimal` como `BsonDouble` en lugar de `BsonDecimal128`).

El conversor se llama **antes** del switch incorporado. Retorna `null` para dejar que el default maneje el valor.

```csharp
// Tipo de dominio
record Money(decimal Amount, string Currency);

// Conversor: almacena Money como BsonDecimal128 del monto
BsonDocument filter = new ValiFlow<Product>()
    .GreaterThan(x => x.Price, new Money(100m, "USD"))
    .ToMongo(value =>
    {
        if (value is Money m)
            return new BsonDecimal128(m.Amount);
        return null; // caer al default para todos los demás tipos
    });
```

Otro caso común — forzar que `decimal` se almacene como `BsonDouble`:

```csharp
BsonDocument filter = new ValiFlow<Order>()
    .GreaterThan(x => x.Total, 250.00m)
    .ToMongo(value =>
    {
        if (value is decimal d)
            return new BsonDouble((double)d);
        return null;
    });
```

---

## Limitaciones

MongoDB soporta todos los tipos de nodos IR. No hay operaciones que lancen `NotSupportedException`.

Una nota de comportamiento: pasar una lista vacía a `.In(...)` produce `{ field: { $in: [] } }`, que MongoDB evalúa como un filtro siempre falso (cero documentos retornados). Esto es consistente con los adaptadores de Elasticsearch y SQL.

Los nombres de campo en el filtro provienen directamente del nombre de la propiedad .NET. Para usar un nombre de campo MongoDB personalizado, aplica `[BsonElement("fieldName")]` en la propiedad de la entidad.

---

## Ejemplo completo

Un filtro realista que combina igualdad, rango, membresía y verificaciones de nulos, integrado con el driver .NET de MongoDB:

```csharp
using MongoDB.Bson;
using MongoDB.Driver;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.MongoDB.Extensions;

// Construir el filtro
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m)
    .LessThan(x => x.Total, 5000m)
    .In(x => x.RegionCode, new[] { "US", "CA", "MX" })
    .IsNotNull(x => x.CustomerId);

BsonDocument mongoFilter = filter.ToMongo();

// Ejecutar con el driver
IMongoCollection<Order> orders = database.GetCollection<Order>("orders");

var results = await orders
    .Find(mongoFilter)
    .Sort(Builders<Order>.Sort.Descending(o => o.CreatedAt))
    .Limit(50)
    .ToListAsync();

// BsonDocument generado (representación lógica):
// {
//   $and: [
//     { Status: "Processing" },
//     { $and: [
//       { Total: { $gte: NumberDecimal("100") } },
//       { $and: [
//         { Total: { $lt: NumberDecimal("5000") } },
//         { $and: [
//           { RegionCode: { $in: ["US", "CA", "MX"] } },
//           { CustomerId: { $ne: null } }
//         ]}
//       ]}
//     ]}
//   ]
// }
```

### Usando la sobrecarga de expresión

Cuando ya tienes un predicado compilado, usa la sobrecarga `Expression<Func<T, bool>>`:

```csharp
Expression<Func<Product, bool>> activeAndAffordable =
    p => p.IsActive && p.Price < 200m;

BsonDocument filter = activeAndAffordable.ToMongo();
var products = await collection.Find(filter).ToListAsync();
```
