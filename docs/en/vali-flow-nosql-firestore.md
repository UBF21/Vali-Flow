# Vali-Flow.NoSql.Firestore — Complete Reference

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Extension Methods](#extension-methods)
5. [Supported Operations](#supported-operations)
6. [Type Mapping](#type-mapping)
7. [Custom Value Converter](#custom-value-converter)
8. [Limitations](#limitations)
9. [Full Example](#full-example)

---

## Overview

**Vali-Flow.NoSql.Firestore** translates a `ValiFlow<T>` expression tree into a native `Google.Cloud.Firestore.Filter`. Unlike the SQL/MongoDB adapters, this one does not build a document or string — it produces the actual `Filter` object from the official `Google.Cloud.Firestore` SDK (v4.4.0), so the output is accepted directly by `Query.Where(filter)` / `CollectionReference.Where(filter)` with no cast, wrapping, or serialization step.

Because the adapter is bound to the real SDK type, it inherits the real SDK's capabilities — and its real gaps. Firestore's query engine is intentionally limited compared to a general-purpose database: there is no pattern-matching operator and no way to negate an arbitrary sub-filter. Both limitations are enforced in code (see [Limitations](#limitations)) rather than silently producing an incorrect filter.

---

## Installation

```bash
dotnet add package Vali-Flow.NoSql.Firestore
```

`Vali-Flow.NoSql` (the IR layer) is included as a transitive dependency. `Google.Cloud.Firestore` 4.4.0 is a direct dependency — this package does not try to abstract it away.

---

## Quick Start

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

## Extension Methods

Both overloads live in `Vali_Flow.NoSql.Firestore.Extensions.ValiFlowFirestoreExtensions`.

### `ToFirestore<T>(this ValiFlow<T> flow, Func<object?, object?>? customConverter = null)`

Translates the conditions accumulated in a `ValiFlow<T>` builder into a native `Filter`.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `flow` | `ValiFlow<T>` | Yes | The builder containing the conditions. Throws `ArgumentNullException` if `null`. |
| `customConverter` | `Func<object?, object?>?` | No | Hook for mapping CLR types not handled by the built-in switch. Return `null` to fall through to the default conversion. Scoped to this call only (thread-safe). |

**Returns:** `Filter` — pass directly to `Query.Where(filter)`.

### `ToFirestore<T>(this Expression<Func<T, bool>> expression, Func<object?, object?>? customConverter = null)`

Same translation for a pre-built `Expression<Func<T, bool>>`.

```csharp
Expression<Func<Order, bool>> expr = o => o.Status == "Pending" && o.Total > 50m;
Filter filter = expr.ToFirestore();
```

Both overloads are thin wrappers over `FirestoreFilterTranslator.Translate(IConditionNode, Func<object?, object?>?)` — the `ValiFlow<T>` overload calls `flow.ToNoSqlIR()` first, the expression overload calls `expression.ToNoSqlIR()` first.

---

## Supported Operations

| ValiFlow method | IR node | Firestore `Filter` call |
|----------------|---------|--------------------------|
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
| `.Contains` / `.StartsWith` / `.EndsWith` | `LikeNode` | **throws** `NotSupportedException` |
| `.Not(inner)` | `NotNode` | **throws** `NotSupportedException` |

`And`/`Or` nest recursively, exactly mirroring the shape of the IR tree — there is no flattening into a single `Filter.And(a, b, c, ...)` call even if the source expression chains more than two conditions; each binary node becomes one nested `Filter.And`/`Filter.Or` call.

---

## Type Mapping

The built-in switch inside `FirestoreFilterTranslator` (method `ToValue`) handles these CLR types before handing the value to `Filter.*`:

| CLR type | Value passed to `Filter.*` |
|----------|------------------------------|
| `null` | `null` |
| `Enum` | `int` (via `Convert.ToInt32(e)`) |
| `DateTimeOffset` | `Timestamp.FromDateTimeOffset(dto)` |
| `DateTime` | `Timestamp.FromDateTime(...)` — converted to UTC first if `dt.Kind != DateTimeKind.Utc` |
| anything else (`string`, `int`, `long`, `double`, `bool`, `decimal`, ...) | passed through unchanged — the Firestore SDK serializes these natively |

Resolution order: `customConverter` (if provided) runs first via `ConditionValueResolver.Resolve`; if it returns `null`, the built-in switch above applies.

---

## Custom Value Converter

Use `customConverter` for domain types the built-in switch doesn't know about, or to override a default (for example, storing a `decimal` as `double` instead of letting the SDK serialize it as-is).

The converter runs **before** the built-in switch. Return `null` to let the default handle the value.

```csharp
// Domain type
record Money(decimal Amount, string Currency);

// Converter: store Money as a double amount
Filter filter = new ValiFlow<Product>()
    .GreaterThan(x => x.Price, new Money(100m, "USD"))
    .ToFirestore(value =>
    {
        if (value is Money m)
            return (double)m.Amount;
        return null; // fall through for everything else
    });
```

---

## Limitations

Firestore is the provider with the most query-engine gaps among the Vali-Flow NoSql adapters, and this package does **not** hide that behind a silently-wrong filter — unsupported nodes throw immediately at translation time.

### 1. No pattern matching — `LikeNode` (Contains / StartsWith / EndsWith)

Firestore has no LIKE, regex, or substring query operator. All three `LikeNode` variants throw, with the specific `LikeOp` named in the message:

```csharp
new ValiFlow<User>().Contains(x => x.Name, "ann").ToFirestore();
// throws NotSupportedException:
// "Firestore does not support pattern-matching queries (LikeOp.Contains).
//  There is no regex/LIKE query operator — filter client-side or use a
//  dedicated search index (e.g. Algolia/Elasticsearch) instead."
```

The same happens for `LikeOp.StartsWith` and `LikeOp.EndsWith` — only the operator name in the message changes. **Workaround:** filter the result set client-side after fetching, or integrate a dedicated search index (Algolia, Elasticsearch, Typesense) for text search use cases.

### 2. No generic NOT over an arbitrary sub-expression — `NotNode`

The Firestore SDK does not expose a "negate this filter" composition — there is no `Filter.Not(filter)`. Any `.Not(inner)` call throws unconditionally, regardless of what `inner` is:

```csharp
new ValiFlow<User>().Not(f => f.EqualTo(x => x.IsActive, true)).ToFirestore();
// throws NotSupportedException:
// "Firestore does not support a generic NOT filter over an arbitrary
//  sub-expression. The SDK only exposes field-level negations
//  (NotEqualTo/NotInArray) via Filter, which are already handled when
//  the negated node is an EqualNode/InNode directly."
```

**Workaround:** don't wrap in `.Not(...)`. Express the negation at the field level instead — use `.NotEqualTo(x => x.Field, v)` directly (which becomes `EqualNode.IsNegated = true` → `Filter.NotEqualTo`), rather than `.Not(f => f.EqualTo(x => x.Field, v))`.

### 3. `.In(...)` has no negated counterpart yet

The SDK exposes `Filter.NotInArray(field, values)`, and the translator's doc comments acknowledge it — but `InNode` in the current IR has no `IsNegated` flag (unlike `EqualNode`), so there is currently no `ValiFlow<T>` method that reaches `Filter.NotInArray`. This is not a thrown exception, just an operation that is not reachable yet from the fluent API against this provider.

### 4. Composite/nested `AND`/`OR` map 1:1, with no flattening

Not a missing feature, but worth knowing: chaining three or more conditions produces nested `Filter.And(a, Filter.And(b, c))` rather than a single flat `Filter.And(a, b, c)`. Firestore accepts nested composite filters, so this is semantically equivalent — just worth knowing if you inspect the generated `Filter` for debugging.

---

## Full Example

A realistic filter combining equality, range, membership, and null checks, executed against a real `FirestoreDb`:

```csharp
using Google.Cloud.Firestore;
using Vali_Flow.Core.Builder;
using Vali_Flow.NoSql.Firestore.Extensions;

// Build the filter
var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Processing")
    .GreaterThanOrEqualTo(x => x.Total, 100m)
    .LessThan(x => x.Total, 5000m)
    .In(x => x.RegionCode, new[] { "US", "CA", "MX" })
    .IsNotNull(x => x.CustomerId);

Filter firestoreFilter = filter.ToFirestore();

// Execute with the real SDK
FirestoreDb firestoreDb = FirestoreDb.Create("my-project-id");
CollectionReference collection = firestoreDb.Collection("orders");
Query query = collection.Where(firestoreFilter).Limit(50);

QuerySnapshot snapshot = await query.GetSnapshotAsync();

foreach (DocumentSnapshot doc in snapshot.Documents)
{
    Order order = doc.ConvertTo<Order>();
}
```

### Using the expression overload

```csharp
Expression<Func<Product, bool>> activeAndAffordable =
    p => p.IsActive && p.Price < 200m;

Filter filter = activeAndAffordable.ToFirestore();
Query query = firestoreDb.Collection("products").Where(filter);
var snapshot = await query.GetSnapshotAsync();
```
