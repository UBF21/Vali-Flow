# Vali-Flow.NoSql.Elasticsearch — Referencia Completa

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

**Vali-Flow.NoSql.Elasticsearch** traduce un árbol de expresiones `ValiFlow<T>` en un objeto `Query` de Elasticsearch (`Elastic.Clients.Elasticsearch.QueryDsl.Query`). La salida se conecta directamente a cualquier operación que acepte una consulta — `Search`, `Count`, `DeleteByQuery`, y más.

El paquete depende únicamente de `Elastic.Clients.Elasticsearch` para los tipos de consulta. No tiene preocupaciones de conexión o ejecución.

---

## Instalación

```bash
dotnet add package Vali-Flow.NoSql.Elasticsearch
```

`Vali-Flow.Core` se incluye como dependencia transitiva.

---

## Inicio rápido

```csharp
using Elastic.Clients.Elasticsearch;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Elasticsearch.Extensions;

var filter = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Age, 18);

Query esFilter = filter.ToElasticsearch();
var results = await client.SearchAsync<User>(s => s.Query(esFilter));
```

---

## Métodos de extensión

Ambas sobrecargas están en `Vali_Flow.NoSql.Elasticsearch.Extensions.ValiFlowElasticsearchExtensions`.

### `ToElasticsearch<T>(this ValiFlow<T> flow, Func<object?, FieldValue?>? customConverter = null)`

Traduce las condiciones acumuladas en un constructor `ValiFlow<T>` en una `Query` de Elasticsearch.

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `flow` | `ValiFlow<T>` | Sí | El constructor que contiene las condiciones. |
| `customConverter` | `Func<object?, FieldValue?>?` | No | Hook para mapear tipos CLR no manejados por el switch incorporado. Retornar `null` para caer al conversor por defecto. |

**Retorna:** `Query` — pásalo directamente a `Search`, `Count`, `DeleteByQuery`, etc.

### `ToElasticsearch<T>(this Expression<Func<T, bool>> expression, Func<object?, FieldValue?>? customConverter = null)`

Misma traducción para una `Expression<Func<T, bool>>` ya construida.

```csharp
Expression<Func<Product, bool>> expr = p => p.Category == "Electronics" && p.Price < 500m;
Query filter = expr.ToElasticsearch();
```

---

## Operaciones soportadas

| Método ValiFlow | Nodo IR | Consulta Elasticsearch |
|----------------|---------|------------------------|
| `.EqualTo(x => x.Field, v)` | `EqualNode(IsNegated: false)` | `term { field: value }` |
| `.NotEqualTo(x => x.Field, v)` | `EqualNode(IsNegated: true)` | `bool { must_not: [term { field: value }] }` |
| `.GreaterThan(x => x.Field, v)` | `ComparisonNode(GT)` | `range { field: { gt: value } }` |
| `.GreaterThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(GTE)` | `range { field: { gte: value } }` |
| `.LessThan(x => x.Field, v)` | `ComparisonNode(LT)` | `range { field: { lt: value } }` |
| `.LessThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(LTE)` | `range { field: { lte: value } }` |
| `.Contains(x => x.Field, "txt")` | `LikeNode(Contains)` | `wildcard { field: "*txt*", case_insensitive: true }` |
| `.StartsWith(x => x.Field, "pre")` | `LikeNode(StartsWith)` | `wildcard { field: "pre*", case_insensitive: true }` |
| `.EndsWith(x => x.Field, "suf")` | `LikeNode(EndsWith)` | `wildcard { field: "*suf", case_insensitive: true }` |
| `.In(x => x.Field, list)` | `InNode` | `terms { field: [...] }` |
| `.IsNull(x => x.Field)` | `NullNode(IsNull)` | `bool { must_not: [exists { field }] }` |
| `.IsNotNull(x => x.Field)` | `NullNode(IsNotNull)` | `exists { field }` |
| `.And(a, b)` | `AndNode` | `bool { must: [a, b] }` |
| `.Or(a, b)` | `OrNode` | `bool { should: [a, b], minimum_should_match: 1 }` |
| `.Not(inner)` | `NotNode` | `bool { must_not: [inner] }` |

Las consultas de rango usan `NumberRangeQuery`, que requiere un campo numérico. El valor se convierte a `double` mediante `Convert.ToDouble`. Pasar un valor no numérico a una comparación de rango lanza `NotSupportedException`.

Los caracteres especiales de comodín (`*`, `?`, `\`) en las cadenas de patrón se escapan antes de construir el patrón de Elasticsearch.

---

## Mapeo de tipos

El switch `ToFieldValue` incorporado maneja estos tipos CLR para consultas term y terms:

| Tipo CLR | `FieldValue` producido |
|----------|-----------------------|
| `null` | `FieldValue.Null` |
| `bool` | `FieldValue.Boolean(b)` |
| `int` | `FieldValue.Long(i)` |
| `long` | `FieldValue.Long(l)` |
| `double` | `FieldValue.Double(d)` |
| `float` | `FieldValue.Double(f)` |
| `decimal` | `FieldValue.Double((double)dec)` |
| `string` | `FieldValue.String(s)` |
| `Enum` | `FieldValue.Long(Convert.ToInt64(e))` |
| cualquier otro | `FieldValue.String(v.ToString())` |

---

## Conversor de valores personalizado

Usa `customConverter` cuando tus tipos de dominio no están en la tabla anterior, o cuando necesitas una representación `FieldValue` diferente a la por defecto.

El conversor se llama **antes** del switch incorporado. Retorna `null` para dejar que el default maneje el valor.

```csharp
// Tipo de dominio
record Money(decimal Amount, string Currency);

// Conversor: representar Money como FieldValue de double
Query filter = new ValiFlow<Product>()
    .LessThan(x => x.Price, new Money(500m, "USD"))
    .ToElasticsearch(value =>
    {
        if (value is Money m)
            return FieldValue.Double((double)m.Amount);
        return null;
    });
```

Forzar que `decimal` use representación de cadena en un campo keyword:

```csharp
Query filter = new ValiFlow<Invoice>()
    .EqualTo(x => x.TaxRate, 0.21m)
    .ToElasticsearch(value =>
    {
        if (value is decimal d)
            return FieldValue.String(d.ToString("G", CultureInfo.InvariantCulture));
        return null;
    });
```

---

## Limitaciones

Elasticsearch soporta todos los tipos de nodos IR. No hay operaciones que lancen `NotSupportedException`.

Notas de comportamiento:

- **IsNull** mapea a `bool { must_not: [exists { field }] }`. Coincide con documentos donde el campo está ausente o es explícitamente `null`, que es el comportamiento estándar de Elasticsearch.
- **Lista In vacía** mapea a `bool { must_not: [match_all {}] }` — una consulta siempre falsa. Ningún documento coincide.
- **Consultas de rango** convierten el valor a `double`. Si el campo almacena fechas, usa `customConverter` para producir una `DateRangeQuery` en su lugar.
- Los nombres de campo provienen directamente del nombre de la propiedad .NET. Para usar un nombre de campo Elasticsearch personalizado, configura tus mappings de índice o la configuración del serializador en consecuencia.

---

## Ejemplo completo

Un filtro realista que combina igualdad, rango, membresía, verificaciones de nulos y composición lógica, ejecutado contra Elasticsearch:

```csharp
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Elasticsearch.Extensions;

// Construir el filtro
var filter = new ValiFlow<Product>()
    .EqualTo(x => x.IsActive, true)
    .In(x => x.Category, new[] { "Electronics", "Computers" })
    .GreaterThanOrEqualTo(x => x.Price, 50m)
    .LessThan(x => x.Price, 1500m)
    .IsNotNull(x => x.Sku);

Query esFilter = filter.ToElasticsearch();

// Ejecutar con el cliente
ElasticsearchClient client = new(new Uri("http://localhost:9200"));

var response = await client.SearchAsync<Product>(s => s
    .Index("products")
    .Query(esFilter)
    .Sort(sort => sort.Field(p => p.Price, f => f.Order(SortOrder.Asc)))
    .From(0)
    .Size(25));

IReadOnlyCollection<Product> products = response.Documents;
```

### Usando la sobrecarga de expresión

```csharp
Expression<Func<Order, bool>> recentLargeOrders =
    o => o.CreatedAt >= DateTime.UtcNow.AddDays(-7) && o.Total > 1000m;

Query filter = recentLargeOrders.ToElasticsearch();

var response = await client.SearchAsync<Order>(s => s
    .Index("orders")
    .Query(filter));
```
