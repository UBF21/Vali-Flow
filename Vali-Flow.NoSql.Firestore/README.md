# Vali-Flow.NoSql.Firestore

Google Cloud Firestore adapter that translates `ValiFlow<T>` filters into a native `Google.Cloud.Firestore.Filter`.

See the [main README](../../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.NoSql.Firestore
```

## Quick Example

```csharp
using Vali_Flow.NoSql.Firestore.Extensions;

var filter = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .GreaterThan(x => x.Age, 18);

Filter firestoreFilter = filter.ToFirestore();

CollectionReference collection = firestoreDb.Collection("users");
Query query = collection.Where(firestoreFilter);
var snapshot = await query.GetSnapshotAsync();
```

## Features

- Translates `ValiFlow<T>` expressions to a native `Filter` (the real SDK type, not a wrapper)
- Depends on the official `Google.Cloud.Firestore` 4.4.0 SDK
- Custom value converters for complex types
- `AndNode`/`OrNode`/`EqualNode`/`ComparisonNode`/`InNode`/`NullNode` all supported

## Limitations

- **No pattern matching.** `Contains`/`StartsWith`/`EndsWith` (`LikeNode`) throw `NotSupportedException` — Firestore has no LIKE/regex query operator.
- **No generic NOT.** `.Not(inner)` (`NotNode`) throws `NotSupportedException` — the SDK exposes no "negate this filter" composition. Only field-level negation via `.NotEqualTo(...)` (`EqualNode.IsNegated`) is supported.

See the [full reference](../../docs/en/vali-flow-nosql-firestore.md) for exact exception messages and workarounds.

## License

MIT
