# Changelog — Vali-Flow.NoSql.CosmosDb

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.0.0] — Initial release

### Added
- `CosmosFilterExpression` — translates Vali-Flow IR nodes into an Azure Cosmos DB SQL API WHERE clause fragment plus a parameter dictionary, with no dependency on the `Microsoft.Azure.Cosmos` SDK
- Support for: equality, comparison, AND/OR/NOT, IN list, CONTAINS/STARTSWITH/ENDSWITH (native Cosmos SQL functions), IS_NULL/NOT IS_NULL
- `ValiFlowCosmosDbExtensions.ToCosmosDb<T>()` — extension methods on `ValiFlow<T>` and `Expression<Func<T, bool>>`
- Custom value converter hook for domain types not covered by the built-in CLR type mapping
- XML documentation on all public types and members
