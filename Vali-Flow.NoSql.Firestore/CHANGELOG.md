# Changelog — Vali-Flow.NoSql.Firestore

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.0.0] — Initial release

### Added
- `FirestoreFilterTranslator` — translates Vali-Flow IR nodes into a native `Google.Cloud.Firestore.Filter` (Google.Cloud.Firestore 4.4.0)
- `ValiFlowFirestoreExtensions.ToFirestore<T>()` — extension methods on `ValiFlow<T>` and on `Expression<Func<T, bool>>`
- Support for: equality (`EqualTo`/`NotEqualTo`), comparison (`GreaterThan`/`GreaterThanOrEqualTo`/`LessThan`/`LessThanOrEqualTo`), `In`, null checks, `And`/`Or`
- Built-in CLR type mapping (`Enum` → `int`, `DateTime`/`DateTimeOffset` → `Timestamp`, pass-through for primitives) with `customConverter` override hook
- XML documentation on all public types and members

### Known limitations
- `Contains`/`StartsWith`/`EndsWith` throw `NotSupportedException` — Firestore has no pattern-matching query operator
- `.Not(inner)` throws `NotSupportedException` — the SDK exposes no generic filter negation
- Negated `.In(...)` (`Filter.NotInArray`) not yet reachable — `InNode` has no `IsNegated` flag
