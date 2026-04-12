# Vali-Flow.NoSql.DynamoDB — Referencia Completa

## Tabla de Contenidos

1. [Descripción general](#descripción-general)
2. [Instalación](#instalación)
3. [Inicio rápido](#inicio-rápido)
4. [Métodos de extensión](#métodos-de-extensión)
5. [DynamoFilterExpression](#dynamofilterexpression)
6. [Operaciones soportadas](#operaciones-soportadas)
7. [Mapeo de tipos](#mapeo-de-tipos)
8. [Conversor de valores personalizado](#conversor-de-valores-personalizado)
9. [Limitaciones](#limitaciones)
10. [Ejemplo completo](#ejemplo-completo)

---

## Descripción general

**Vali-Flow.NoSql.DynamoDB** traduce un árbol de expresiones `ValiFlow<T>` en una cadena `FilterExpression` de DynamoDB junto con los diccionarios `ExpressionAttributeNames` y `ExpressionAttributeValues` requeridos por `ScanRequest` y `QueryRequest`.

El paquete depende únicamente de `AWSSDK.DynamoDBv2`. Es un constructor de consultas puro sin preocupaciones de conexión o ejecución.

---

## Instalación

```bash
dotnet add package Vali-Flow.NoSql.DynamoDB
```

`Vali-Flow.Core` se incluye como dependencia transitiva.

---

## Inicio rápido

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.DynamoDB.Extensions;
using Vali_Flow.NoSql.DynamoDB.Models;

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

DynamoFilterExpression f = filter.ToDynamoDB();

var request = new ScanRequest
{
    TableName                 = "Orders",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};

ScanResponse response = await client.ScanAsync(request);
```

---

## Métodos de extensión

Ambas sobrecargas están en `Vali_Flow.NoSql.DynamoDB.Extensions.ValiFlowDynamoExtensions`.

### `ToDynamoDB<T>(this ValiFlow<T> flow, Func<object?, AttributeValue?>? customConverter = null)`

Traduce las condiciones acumuladas en un constructor `ValiFlow<T>` en un `DynamoFilterExpression`.

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `flow` | `ValiFlow<T>` | Sí | El constructor que contiene las condiciones. |
| `customConverter` | `Func<object?, AttributeValue?>?` | No | Hook para mapear tipos CLR no manejados por el switch incorporado. Retornar `null` para caer al conversor por defecto. |

**Retorna:** `DynamoFilterExpression` — aplícalo a `ScanRequest` o `QueryRequest`.

### `ToDynamoDB<T>(this Expression<Func<T, bool>> expression, Func<object?, AttributeValue?>? customConverter = null)`

Misma traducción para una `Expression<Func<T, bool>>` ya construida.

```csharp
Expression<Func<User, bool>> expr = u => u.IsActive && u.Age >= 21;
DynamoFilterExpression f = expr.ToDynamoDB();
```

---

## DynamoFilterExpression

`DynamoFilterExpression` es una clase sellada que encapsula los tres valores que DynamoDB necesita:

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `FilterExpression` | `string` | La cadena de expresión, p.ej. `"(#f0 = :v0 AND #f1 > :v1)"`. |
| `ExpressionAttributeNames` | `IReadOnlyDictionary<string, string>` | Mapea marcadores de posición (`#f0`, `#f1`, …) a nombres de atributo reales. |
| `ExpressionAttributeValues` | `IReadOnlyDictionary<string, AttributeValue>` | Mapea marcadores de posición (`:v0`, `:v1`, …) a instancias de `AttributeValue`. |

El traductor genera marcadores secuenciales (`#f0`, `#f1`, … para nombres; `:v0`, `:v1`, … para valores) para evitar colisiones y eludir conflictos con palabras reservadas de DynamoDB.

`IReadOnlyDictionary<K,V>` no implementa `IDictionary<K,V>` directamente. Llama a `.ToDictionary()` cuando el SDK requiere `Dictionary<string, string>` o `Dictionary<string, AttributeValue>`.

---

## Operaciones soportadas

| Método ValiFlow | Nodo IR | FilterExpression DynamoDB |
|----------------|---------|---------------------------|
| `.EqualTo(x => x.Field, v)` | `EqualNode(IsNegated: false)` | `#f0 = :v0` |
| `.NotEqualTo(x => x.Field, v)` | `EqualNode(IsNegated: true)` | `#f0 <> :v0` |
| `.GreaterThan(x => x.Field, v)` | `ComparisonNode(GT)` | `#f0 > :v0` |
| `.GreaterThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(GTE)` | `#f0 >= :v0` |
| `.LessThan(x => x.Field, v)` | `ComparisonNode(LT)` | `#f0 < :v0` |
| `.LessThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(LTE)` | `#f0 <= :v0` |
| `.Contains(x => x.Field, "txt")` | `LikeNode(Contains)` | `contains(#f0, :v0)` |
| `.StartsWith(x => x.Field, "pre")` | `LikeNode(StartsWith)` | `begins_with(#f0, :v0)` |
| `.EndsWith(x => x.Field, "suf")` | `LikeNode(EndsWith)` | lanza `NotSupportedException` |
| `.In(x => x.Field, list)` | `InNode` | `#f0 IN (:v0, :v1, …)` |
| `.IsNull(x => x.Field)` | `NullNode(IsNull)` | `attribute_not_exists(#f0)` |
| `.IsNotNull(x => x.Field)` | `NullNode(IsNotNull)` | `attribute_exists(#f0)` |
| `.And(a, b)` | `AndNode` | `(a AND b)` |
| `.Or(a, b)` | `OrNode` | `(a OR b)` |
| `.Not(inner)` | `NotNode` | `NOT (inner)` |

---

## Mapeo de tipos

El switch `ToAttributeValue` incorporado maneja estos tipos CLR:

| Tipo CLR | `AttributeValue` producido |
|----------|---------------------------|
| `null` | `{ NULL = true }` |
| `bool` | `{ BOOL = b }` |
| `string` | `{ S = s }` |
| `int` | `{ N = "value" }` |
| `long` | `{ N = "value" }` |
| `double` | `{ N = "value" }` (InvariantCulture) |
| `float` | `{ N = "value" }` (InvariantCulture) |
| `decimal` | `{ N = "value" }` (InvariantCulture) |
| `Guid` | `{ S = g.ToString() }` |
| `Enum` | `{ N = "underlying_int64" }` |
| cualquier otro | `{ S = v.ToString() }` |

---

## Conversor de valores personalizado

Usa `customConverter` cuando tus tipos de dominio no están en la tabla anterior o cuando necesitas una representación `AttributeValue` diferente a la por defecto.

El conversor se llama **antes** del switch incorporado. Retorna `null` para dejar que el default maneje el valor.

```csharp
// Tipo de dominio
record Money(decimal Amount, string Currency);

// Conversor: almacena Money como número (solo el monto)
DynamoFilterExpression f = new ValiFlow<Order>()
    .GreaterThan(x => x.Total, new Money(500m, "USD"))
    .ToDynamoDB(value =>
    {
        if (value is Money m)
            return new AttributeValue { N = m.Amount.ToString(CultureInfo.InvariantCulture) };
        return null;
    });
```

Almacenando un `DateTimeOffset` como cadena ISO-8601 en DynamoDB:

```csharp
DynamoFilterExpression f = new ValiFlow<Event>()
    .GreaterThan(x => x.StartsAt, DateTimeOffset.UtcNow)
    .ToDynamoDB(value =>
    {
        if (value is DateTimeOffset dto)
            return new AttributeValue { S = dto.ToString("O") };
        return null;
    });
```

---

## Limitaciones

| Limitación | Detalle |
|-----------|---------|
| `EndsWith` | Lanza `NotSupportedException`. DynamoDB no tiene función de comodín al final. Usa `Contains` o `StartsWith`, o aplica el filtro del lado del cliente tras la recuperación. |
| Lista `In` > 100 valores | Lanza `InvalidOperationException`. El operador `IN` de DynamoDB soporta máximo 100 operandos. Divide la consulta o procesa los valores en lotes. |
| Lista `In` vacía | Produce `(attribute_exists(#f0) AND attribute_not_exists(#f0))` — una expresión siempre falsa. Ningún ítem coincide. |
| Llamada a `.ToDictionary()` requerida | `ExpressionAttributeNames` y `ExpressionAttributeValues` son `IReadOnlyDictionary<K,V>`. Los constructores de `ScanRequest` / `QueryRequest` requieren `Dictionary<K,V>`. Llama a `.ToDictionary()` en cada propiedad antes de asignar. |

---

## Ejemplo completo

Un filtro realista que combina igualdad, rango, membresía y verificaciones de nulos, aplicado a un `ScanRequest` y `QueryRequest` de DynamoDB:

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.DynamoDB.Extensions;
using Vali_Flow.NoSql.DynamoDB.Models;

IAmazonDynamoDB client = new AmazonDynamoDBClient();

// Construir el filtro
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m)
    .LessThan(x => x.Total, 5000m)
    .In(x => x.RegionCode, new[] { "US", "CA", "MX" })
    .IsNotNull(x => x.CustomerId);

DynamoFilterExpression f = filter.ToDynamoDB();

// Usar en un Scan
var scanRequest = new ScanRequest
{
    TableName                 = "Orders",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};

ScanResponse scanResponse = await client.ScanAsync(scanRequest);

// Usar en un Query (cuando se conoce la partition key)
var queryRequest = new QueryRequest
{
    TableName                 = "Orders",
    KeyConditionExpression    = "#pk = :pkval",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = new Dictionary<string, string>(f.ExpressionAttributeNames)
                                { ["#pk"] = "CustomerId" },
    ExpressionAttributeValues = new Dictionary<string, AttributeValue>(f.ExpressionAttributeValues)
                                { [":pkval"] = new AttributeValue { S = "cust-001" } }
};

QueryResponse queryResponse = await client.QueryAsync(queryRequest);
```

### Manejando la limitación de EndsWith del lado del cliente

```csharp
// DynamoDB no puede filtrar con EndsWith — recuperar resultados más amplios y filtrar en memoria
var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .StartsWith(x => x.Sku, "PROD-");   // reducir con StartsWith

DynamoFilterExpression f = filter.ToDynamoDB();

var scanRequest = new ScanRequest
{
    TableName                 = "Products",
    FilterExpression          = f.FilterExpression,
    ExpressionAttributeNames  = f.ExpressionAttributeNames.ToDictionary(),
    ExpressionAttributeValues = f.ExpressionAttributeValues.ToDictionary()
};

ScanResponse response = await client.ScanAsync(scanRequest);

// Aplicar EndsWith del lado del cliente
var results = response.Items
    .Where(item => item["Sku"].S.EndsWith("-V2"))
    .ToList();
```
