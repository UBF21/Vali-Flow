# Vali-Flow.NoSql.Firestore — Referencia Completa

## Tabla de Contenidos

1. [Resumen](#resumen)
2. [Instalación](#instalación)
3. [Inicio Rápido](#inicio-rápido)
4. [Métodos de Extensión](#métodos-de-extensión)
5. [Operaciones Soportadas](#operaciones-soportadas)
6. [Mapeo de Tipos](#mapeo-de-tipos)
7. [Conversor de Valores Personalizado](#conversor-de-valores-personalizado)
8. [Limitaciones](#limitaciones)
9. [Ejemplo Completo](#ejemplo-completo)

---

## Resumen

**Vali-Flow.NoSql.Firestore** traduce un árbol de expresiones `ValiFlow<T>` a un `Google.Cloud.Firestore.Filter` nativo. A diferencia de los adaptadores SQL/MongoDB, este paquete no construye un documento ni un string — produce el objeto `Filter` real del SDK oficial `Google.Cloud.Firestore` (v4.4.0), así que el resultado se pasa directo a `Query.Where(filter)` / `CollectionReference.Where(filter)` sin ningún cast, wrapping ni paso de serialización.

Como el adaptador está atado al tipo real del SDK, hereda tanto las capacidades reales del SDK como sus huecos reales. El motor de queries de Firestore es intencionalmente limitado comparado con una base de datos de propósito general: no hay operador de pattern-matching y no hay forma de negar un sub-filtro arbitrario. Ambas limitaciones están forzadas en el código (ver [Limitaciones](#limitaciones)) en vez de producir silenciosamente un filtro incorrecto.

---

## Instalación

```bash
dotnet add package Vali-Flow.NoSql.Firestore
```

`Vali-Flow.NoSql` (la capa de IR) se incluye como dependencia transitiva. `Google.Cloud.Firestore` 4.4.0 es una dependencia directa — este paquete no intenta abstraerlo.

---

## Inicio Rápido

```csharp
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Firestore.Extensions;
using Google.Cloud.Firestore;

var filter = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Age, 18);

Filter firestoreFilter = filter.ToFirestore();

FirestoreDb firestoreDb = FirestoreDb.Create("my-project-id");
CollectionReference collection = firestoreDb.Collection("users");
Query query = collection.Where(firestoreFilter);
QuerySnapshot snapshot = await query.GetSnapshotAsync();

foreach (DocumentSnapshot doc in snapshot.Documents)
{
    var user = doc.ConvertTo<User>();
}
```

---

## Métodos de Extensión

Ambas sobrecargas viven en `Vali_Flow.NoSql.Firestore.Extensions.ValiFlowFirestoreExtensions`.

### `ToFirestore<T>(this ValiFlow<T> flow, Func<object?, object?>? customConverter = null)`

Traduce las condiciones acumuladas en un builder `ValiFlow<T>` a un `Filter` nativo.

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|--------------|
| `flow` | `ValiFlow<T>` | Sí | El builder con las condiciones. Lanza `ArgumentNullException` si es `null`. |
| `customConverter` | `Func<object?, object?>?` | No | Hook para mapear tipos CLR que el switch incorporado no maneja. Retornar `null` para caer en la conversión por defecto. Está acotado a esta llamada (thread-safe). |

**Retorna:** `Filter` — pasalo directo a `Query.Where(filter)`.

### `ToFirestore<T>(this Expression<Func<T, bool>> expression, Func<object?, object?>? customConverter = null)`

Misma traducción para un `Expression<Func<T, bool>>` ya construido.

```csharp
Expression<Func<Order, bool>> expr = o => o.Status == "Pending" && o.Total > 50m;
Filter filter = expr.ToFirestore();
```

Ambas sobrecargas son wrappers delgados sobre `FirestoreFilterTranslator.Translate(IConditionNode, Func<object?, object?>?)` — la sobrecarga de `ValiFlow<T>` llama primero a `flow.ToNoSqlIR()`, y la de expresión llama primero a `expression.ToNoSqlIR()`.

---

## Operaciones Soportadas

| Método ValiFlow | Nodo IR | Llamada a `Filter` de Firestore |
|------------------|---------|-----------------------------------|
| `.EqualTo(x => x.Field, v)` | `EqualNode(IsNegated: false)` | `Filter.EqualTo(field, value)` |
| `.NotEqualTo(x => x.Field, v)` | `EqualNode(IsNegated: true)` | `Filter.NotEqualTo(field, value)` |
| `.GreaterThan(x => x.Field, v)` | `ComparisonNode(GreaterThan)` | `Filter.GreaterThan(field, value)` |
| `.GreaterThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(GreaterThanOrEqual)` | `Filter.GreaterThanOrEqualTo(field, value)` |
| `.LessThan(x => x.Field, v)` | `ComparisonNode(LessThan)` | `Filter.LessThan(field, value)` |
| `.LessThanOrEqualTo(x => x.Field, v)` | `ComparisonNode(LessThanOrEqual)` | `Filter.LessThanOrEqualTo(field, value)` |
| `.In(x => x.Field, list)` | `InNode` | `Filter.InArray(field, values)` |
| `.IsNull(x => x.Field)` | `NullNode(IsNull)` | `Filter.EqualTo(field, null)` |
| `.IsNotNull(x => x.Field)` | `NullNode(IsNotNull)` | `Filter.NotEqualTo(field, null)` |
| `.And(a, b)` | `AndNode` | `Filter.And(left, right)` |
| `.Or(a, b)` | `OrNode` | `Filter.Or(left, right)` |
| `.Contains` / `.StartsWith` / `.EndsWith` | `LikeNode` | **lanza** `NotSupportedException` |
| `.Not(inner)` | `NotNode` | **lanza** `NotSupportedException` |

`And`/`Or` anidan recursivamente, replicando exactamente la forma del árbol IR — no hay un "flattening" a un único `Filter.And(a, b, c, ...)` aunque la expresión original encadene más de dos condiciones; cada nodo binario se convierte en una llamada anidada `Filter.And`/`Filter.Or`.

---

## Mapeo de Tipos

El switch incorporado dentro de `FirestoreFilterTranslator` (método `ToValue`) maneja estos tipos CLR antes de pasar el valor a `Filter.*`:

| Tipo CLR | Valor pasado a `Filter.*` |
|----------|------------------------------|
| `null` | `null` |
| `Enum` | `int` (vía `Convert.ToInt32(e)`) |
| `DateTimeOffset` | `Timestamp.FromDateTimeOffset(dto)` |
| `DateTime` | `Timestamp.FromDateTime(...)` — convertido a UTC primero si `dt.Kind != DateTimeKind.Utc` |
| cualquier otro (`string`, `int`, `long`, `double`, `bool`, `decimal`, ...) | pasa sin cambios — el SDK de Firestore los serializa de forma nativa |

Orden de resolución: `customConverter` (si se provee) corre primero vía `ConditionValueResolver.Resolve`; si retorna `null`, aplica el switch incorporado de arriba.

---

## Conversor de Valores Personalizado

Usá `customConverter` para tipos de dominio que el switch incorporado no conoce, o para sobreescribir un default (por ejemplo, almacenar un `decimal` como `double` en vez de dejar que el SDK lo serialice tal cual).

El conversor corre **antes** del switch incorporado. Retorná `null` para dejar que el default maneje el valor.

```csharp
// Tipo de dominio
record Money(decimal Amount, string Currency);

// Conversor: almacenar Money como un double del monto
Filter filter = new ValiFlow<Product>()
    .GreaterThan(x => x.Price, new Money(100m, "USD"))
    .ToFirestore(value =>
    {
        if (value is Money m)
            return (double)m.Amount;
        return null; // dejar pasar todo lo demás
    });
```

---

## Limitaciones

Firestore es el proveedor con más huecos reales en su motor de queries entre los adaptadores NoSql de Vali-Flow, y este paquete **no** lo esconde detrás de un filtro silenciosamente incorrecto — los nodos no soportados lanzan de inmediato al momento de traducir.

### 1. Sin pattern matching — `LikeNode` (Contains / StartsWith / EndsWith)

Firestore no tiene operador de query LIKE, regex, ni de substring. Las tres variantes de `LikeNode` lanzan, con el `LikeOp` específico nombrado en el mensaje:

```csharp
new ValiFlow<User>().Contains(x => x.Name, "ana").ToFirestore();
// lanza NotSupportedException:
// "Firestore does not support pattern-matching queries (LikeOp.Contains).
//  There is no regex/LIKE query operator — filter client-side or use a
//  dedicated search index (e.g. Algolia/Elasticsearch) instead."
```

Lo mismo pasa con `LikeOp.StartsWith` y `LikeOp.EndsWith` — solo cambia el nombre del operador en el mensaje. **Workaround:** filtrá el resultado del lado del cliente después de obtenerlo, o integrá un índice de búsqueda dedicado (Algolia, Elasticsearch, Typesense) para casos de búsqueda de texto.

### 2. Sin NOT genérico sobre un sub-árbol arbitrario — `NotNode`

El SDK de Firestore no expone una composición de "negar este filtro" — no existe un `Filter.Not(filter)`. Cualquier llamada a `.Not(inner)` lanza incondicionalmente, sin importar qué sea `inner`:

```csharp
new ValiFlow<User>().Not(f => f.EqualTo(x => x.IsActive, true)).ToFirestore();
// lanza NotSupportedException:
// "Firestore does not support a generic NOT filter over an arbitrary
//  sub-expression. The SDK only exposes field-level negations
//  (NotEqualTo/NotInArray) via Filter, which are already handled when
//  the negated node is an EqualNode/InNode directly."
```

**Workaround:** no envuelvas en `.Not(...)`. Expresá la negación a nivel de campo directamente — usá `.NotEqualTo(x => x.Field, v)` (que se convierte en `EqualNode.IsNegated = true` → `Filter.NotEqualTo`), en vez de `.Not(f => f.EqualTo(x => x.Field, v))`.

### 3. `.In(...)` todavía no tiene contraparte negada

El SDK expone `Filter.NotInArray(field, values)`, y los comentarios de documentación del traductor lo reconocen — pero `InNode` en el IR actual no tiene un flag `IsNegated` (a diferencia de `EqualNode`), así que por ahora no hay ningún método de `ValiFlow<T>` que llegue a `Filter.NotInArray`. Esto no es una excepción lanzada, simplemente es una operación que todavía no es alcanzable desde la API fluida contra este proveedor.

### 4. `AND`/`OR` compuestos y anidados mapean 1:1, sin flattening

No es una funcionalidad faltante, pero vale saberlo: encadenar tres o más condiciones produce un `Filter.And(a, Filter.And(b, c))` anidado en vez de un único `Filter.And(a, b, c)` plano. Firestore acepta filtros compuestos anidados, así que es semánticamente equivalente — solo vale tenerlo presente si inspeccionás el `Filter` generado para debuggear.

---

## Ejemplo Completo

Un filtro realista que combina igualdad, rango, pertenencia y chequeos de null, ejecutado contra un `FirestoreDb` real:

```csharp
using Google.Cloud.Firestore;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Firestore.Extensions;

// Construir el filtro
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m)
    .LessThan(x => x.Total, 5000m)
    .In(x => x.RegionCode, new[] { "US", "CA", "MX" })
    .IsNotNull(x => x.CustomerId);

Filter firestoreFilter = filter.ToFirestore();

// Ejecutar con el SDK real
FirestoreDb firestoreDb = FirestoreDb.Create("my-project-id");
CollectionReference collection = firestoreDb.Collection("orders");
Query query = collection.Where(firestoreFilter).Limit(50);

QuerySnapshot snapshot = await query.GetSnapshotAsync();

foreach (DocumentSnapshot doc in snapshot.Documents)
{
    Order order = doc.ConvertTo<Order>();
}
```

### Usando la sobrecarga de expresión

```csharp
Expression<Func<Product, bool>> activeAndAffordable =
    p => p.IsActive && p.Price < 200m;

Filter filter = activeAndAffordable.ToFirestore();
Query query = firestoreDb.Collection("products").Where(filter);
var snapshot = await query.GetSnapshotAsync();
```
