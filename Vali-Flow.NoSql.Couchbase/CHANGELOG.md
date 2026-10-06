# Changelog — Vali-Flow.NoSql.Couchbase

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.0.0] — Initial release

### Added
- `CouchbaseFilterExpression` — sealed class exposing `WhereClause` (string, no leading `WHERE`) and `Parameters` (`IReadOnlyDictionary<string, object?>`, named `$p0`, `$p1`, …)
- `ValiFlowCouchbaseExtensions.ToCouchbase<T>()` — extension method on `ValiFlow<T>` and on a pre-built `Expression<Func<T, bool>>`
- Support for: equality, comparison (`GreaterThan`/`GreaterThanOrEqualTo`/`LessThan`/`LessThanOrEqualTo`), AND/OR/NOT, IN (bound as a single array parameter), LIKE (`Contains`/`StartsWith`/`EndsWith`), null checks
- `customConverter` hook for mapping CLR types not handled by the built-in value-resolution switch
- No dependency on the Couchbase .NET SDK — pure N1QL text/parameter builder, no connection or execution concerns
