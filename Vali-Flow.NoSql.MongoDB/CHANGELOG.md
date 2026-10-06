# Changelog — Vali-Flow.NoSql.MongoDB

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.0.0] — Initial release

### Added
- `MongoFilterTranslator` — translates Vali-Flow IR nodes into a MongoDB `BsonDocument` filter (depends only on `MongoDB.Bson`, not the full `MongoDB.Driver`)
- `ValiFlowMongoExtensions.ToMongo<T>()` — extension method on `ValiFlow<T>` and on `Expression<Func<T, bool>>`, with an optional `customConverter` hook for CLR types not handled by the built-in switch
- Support for: equality/inequality, comparison (GT/GTE/LT/LTE), Contains/StartsWith/EndsWith (case-insensitive regex, special characters escaped), IN list, null checks, AND/OR/NOT
- XML documentation on all public types and members

### Fixed
- Closure-captured `null` values now translate to the same `{ field: null }` node as a literal `null` (were previously inconsistent).
- `.In(...)` value lists are now capped at 10,000 entries. See the root [CHANGELOG.md](../CHANGELOG.md) `[Unreleased]` entry for details.
