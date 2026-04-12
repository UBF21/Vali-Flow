# Changelog — Vali-Flow.NoSql.MongoDB

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.0.0] — Initial release

### Added
- `MongoFilterTranslator` — translates Vali-Flow IR nodes into MongoDB `FilterDefinition<T>` (MongoDB.Driver)
- Support for: equality, comparison, AND/OR/NOT, IN list, LIKE (regex-based), null checks
- `ValiFlowMongoExtensions.ToMongoFilter<T>()` — extension method on `ValiFlow<T>`
- XML documentation on all public types and members
