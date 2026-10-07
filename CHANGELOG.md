# Changelog

**English** | [Español](CHANGELOG.es.md)

All notable changes to the Vali-Flow ecosystem are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## Vali-Flow.Abstractions — 1.2.0

### Added

- **`ValiFlowDiagnostics`** — shared `ActivitySource`-based `StartActivity`/`RecordException` helper for OpenTelemetry-compatible tracing, now wired through the EF Core evaluator, `Vali-Flow.InMemory`, `Vali-Flow.Sql`'s six builders, and the Mongo/DynamoDB/Elasticsearch/Redis translators.

---

## Vali-Flow — 1.4.0

### Added

- **`GenericRepository<T, TKey>`** — a thin repository wrapping `ValiFlowEvaluator<T>` with `GetById`/`GetAll`/`GetPaged`/`Add`/`Update`/`Delete`/`SaveChanges`, for consumers who want a conventional repository surface instead of calling the evaluator directly.
- EF Core benchmark using the `EFCore.InMemory` provider (`Vali-Flow.Benchmarks`).
- `ValiFlowDiagnostics` tracing wired into the evaluator's read/write paths.

### Fixed

- `UpsertRangeAsync` dropped entries on duplicate keys (list accumulator instead of dictionary) — fixed to a dictionary accumulator.
- `UpsertRangeAsync.PartitionUpsertRange` could discard a tracked entity's primary key — now preserved.
- `BulkInsertOrUpdateAsync` could race under concurrent calls for the same entity type + key columns — calls are now serialized per that combination.
- **Bulk upsert contention under concurrency**: replaced the ad-hoc retry with a generic transient-retry policy (`BulkUpsertRetryPolicy`) covering SQL Server, PostgreSQL, MySQL, and SQLite, gated per table (not per `UpdateByProperties` key set) to avoid serializing unrelated upserts against each other. Retry budget raised in two steps (4 → 7 → 12 attempts) based on observed contention under load.
- Eliminated a flaky `Activity`-capture test (timing-dependent assertion replaced with deterministic capture).

### Changed

- Query-construction logic extracted from `ValiFlowEvaluator` into a dedicated `QuerySpecificationBuilder` (single-responsibility split, no public API change).
- Test coverage raised from 74.37% to 97.42%.

---

## Vali-Flow.InMemory — 1.2.0

### Added

- `ValiFlowDiagnostics` tracing wired into the evaluator's read/write paths.

### Fixed

- `Update(entity, externalList)` could leak the updated entity into the internal store on a later bare `SaveChanges()` call.
- Eliminated a flaky `Activity`-capture test (timing-dependent assertion replaced with deterministic capture).

### Changed

- `ValiFlowEvaluator<T, TProperty>` (904 lines) split into `Bridge`/`Read`/`Write`/`Grouped` partial files; no public API change.

---

## Vali-Flow.Sql — 1.2.0

### Added

- `SqlInsertBuilder`/`SqlUpdateBuilder.SetAllFrom(entity, exclude...)` — reflection-based bulk column mapper.
- `ValiFlowDiagnostics` tracing wired into all six builders.

### Fixed

- **Security — SQL identifier injection**: the builders interpolated table/schema/type names directly; added `SqlIdentifierGuard` (regex whitelist) validating all identifiers before interpolation.
- `OrIgnore`/`OrReplace` dialect guard bug, `Set()`-after-`SelectFrom()` silent data loss, `Where()` double-call silently overwriting the first condition instead of combining — all fixed.
- Added `AllowDeleteUnmatched()` guard required before `WhenNotMatchedBySourceDelete` in `SqlMergeBuilder` (prevents accidental unguarded deletes in a MERGE).
- Eliminated a flaky `Activity`-capture test (timing-dependent assertion replaced with deterministic capture).

---

## Vali-Flow.NoSql — 1.1.1

### Added

- `ValiFlowDiagnostics` tracing wired into the shared translator base used by the Mongo and Elasticsearch providers.

### Fixed

- Closure-captured `null` values in a `ValiFlow<T>` condition weren't translated to the same `NullNode` as a literal `null`, producing inconsistent filters between `.EqualTo(x => x.Prop, someNullVariable)` and `.EqualTo(x => x.Prop, null)`.
- Unbounded `IN` value lists could exceed the engine's own limits; added `MaxInValues` caps (Mongo 10,000; Elasticsearch 65,536, matching `index.max_terms_count`).

---

## Vali-Flow.NoSql.MongoDB — 1.2.0

### Added

- `ValiFlowDiagnostics` tracing wired into `MongoFilterTranslator`.

---

## Vali-Flow.NoSql.Elasticsearch — 1.2.0

### Added

- `ValiFlowDiagnostics` tracing wired into `ElasticsearchFilterTranslator`.

### Fixed

- Swallowed inner exception in `ToDouble` conversion — failures surfaced as a generic error with no root cause.

---

## Vali-Flow.NoSql.Redis — 1.2.0

### Added

- `ValiFlowDiagnostics` tracing wired into `RedisSearchFilterTranslator`.

### Fixed

- **Security — RediSearch query injection**: special query-syntax characters (e.g. `)`/`|`) weren't escaped in `LIKE` patterns before wrapping them in wildcards, allowing query-structure injection. Now escaped before wildcarding.
- `customConverter` wasn't applied in `VisitComparison`/numeric `VisitIn`, silently ignoring custom type mappings for comparison and `IN` conditions.

---

## Vali-Flow.NoSql.DynamoDB — 1.2.0

### Added

- `ValiFlowDiagnostics` tracing wired into `DynamoFilterTranslator`.

---

## Vali-Flow.NoSql.Couchbase — 1.1.0 *(initial release)*

### Added

- `CouchbaseFilterTranslator` — translates `ValiFlow<T>` IR nodes to a N1QL/SQL++ WHERE fragment, returned as `CouchbaseFilterExpression { WhereClause, Parameters }` — no Couchbase SDK dependency, the consumer applies the fragment with their own client.
- `ValiFlowCouchbaseExtensions` — extension method on `ValiFlow<T>`.

### Fixed

- `decimal` values were bound as N1QL string literals instead of native numbers.

---

## Vali-Flow.NoSql.CosmosDb — 1.1.0 *(initial release)*

### Added

- `CosmosFilterTranslator` — translates `ValiFlow<T>` IR nodes to an Azure Cosmos DB SQL API WHERE fragment, returned as `CosmosFilterExpression { WhereClause, Parameters }` — no `Microsoft.Azure.Cosmos` dependency, the consumer applies the fragment with their own client.
- `ValiFlowCosmosExtensions` — extension method on `ValiFlow<T>`.

---

## Vali-Flow.NoSql.Firestore — 1.1.0 *(initial release)*

### Added

- `FirestoreFilterTranslator` — translates `ValiFlow<T>` IR nodes to a native Google Cloud Firestore `Filter`, depending on the official `Google.Cloud.Firestore` SDK.
- `ValiFlowFirestoreExtensions` — extension method on `ValiFlow<T>`.
- Note: `LikeNode` (all 3 ops) and the generic `NotNode` throw `NotSupportedException` — the Firestore SDK has no pattern-matching query and no NOT over an arbitrary sub-filter, only field-level negation (`EqualNode.IsNegated` → `NotEqualTo`/`NotInArray`).

---

## Vali-Flow — 1.3.3

### Fixed
- Removed `sealed` modifier from all `ValiFlowEvaluator<T>` partial declarations:
  - ValiFlowEvaluator.cs (main)
  - ValiFlowEvaluator.Read.cs
  - ValiFlowEvaluator.Write.cs
  - ValiFlowEvaluator.Aggregates.cs
  - ValiFlowEvaluator.Grouped.cs
  - ValiFlowEvaluator.Bridge.cs

---

## Vali-Flow.InMemory — 1.1.4

### Fixed
- Removed `sealed` modifier from all classes for full inheritance support:
  - `ValiFlowEvaluator<T, TProperty>` (main evaluator)
  - `AsyncInMemoryAdapter<T, TProperty>` (async wrapper)
  - `InMemoryWriteStore<T, TProperty>` (write operations store)
  - `PagedResult<T>` (pagination result model)

Users can now create custom implementations by extending any of these classes.

---

## Vali-Flow — 1.1.0

### Added
- `EvaluatePagedAsync` returns a `PagedResult<T>` with `Items`, `TotalCount`, `TotalPages`, `HasNextPage`, and `HasPreviousPage`
- `EvaluateTopByGroupAsync` — top-N entities per group key
- `EvaluateDistinctAsync` and `EvaluateDuplicatesAsync` — distinct and duplicate detection by key selector
- `EvaluateAggregateAsync` — generic aggregation over any numeric selector
- `EvaluateGroupedAsync`, `EvaluateCountByGroupAsync`, `EvaluateSumByGroupAsync`, `EvaluateMinByGroupAsync`, `EvaluateMaxByGroupAsync`, `EvaluateAverageByGroupAsync` — full grouped aggregate surface
- `ExecuteUpdateAsync` — bulk in-place update via `ExecuteUpdate` (no entity load)
- `ExecuteTransactionAsync` — wraps multiple operations in a single EF Core transaction
- `BulkInsertAsync`, `BulkUpdateAsync`, `BulkDeleteAsync`, `BulkInsertOrUpdateAsync` — high-throughput bulk operations via `EFCore.BulkExtensions`
- `UpsertRangeAsync` — batch upsert with configurable key selector
- `DeleteByConditionAsync` — delete by raw predicate without loading entities
- `EvaluateGetLastAsync` / `EvaluateGetLastFailedAsync` — last-match retrieval
- `QuerySpecification<T>.WithTop`, `WithPagination`, `WithOrderBy`, `AddThenBy`, `WithValiSort` — full fluent spec builder
- `BasicSpecification<T>` — simplified spec without ordering/pagination
- XML documentation on all public types and members

### Changed
- `ValiFlowEvaluator<T>` is now a `sealed partial class` split across focused files (Read, Write, Grouped, Aggregates, Bridge)
- Improved exception messages for invalid spec combinations (Top + Pagination, Pagination without OrderBy)
- `EvaluateAllAsync` and `EvaluateAllFailedAsync` marked `[Obsolete]` — use `EvaluateQueryAsync` / `EvaluateQueryFailedAsync`
- License changed from Apache-2.0 to MIT

### Fixed
- Thread-safety issues in concurrent evaluator usage
- Cache and DRY violations in `UpsertCore` deferred execution
- CS87xx nullable warnings across evaluator and specification classes

---

## Vali-Flow.InMemory — 1.0.0 *(initial release)*

### Added
- `ValiFlowEvaluator<T, TProperty>` — synchronous in-memory evaluator over `IEnumerable<T>`
- Full read surface: `Evaluate`, `EvaluateAny`, `EvaluateCount`, `GetFirst`, `GetLast`, `EvaluateAll`, `EvaluateAllFailed`, `EvaluatePaged`, `EvaluatePagedResult`, `EvaluateTop`, `EvaluateDistinct`, `EvaluateDuplicates`, `GetFirstMatchIndex`, `GetLastMatchIndex`
- Full aggregate surface: `EvaluateMin`, `EvaluateMax`, `EvaluateAverage`, `EvaluateSum`, `EvaluateAggregate`
- Full grouped surface: `EvaluateGrouped`, `EvaluateCountByGroup`, `EvaluateSumByGroup`, `EvaluateMinByGroup`, `EvaluateMaxByGroup`, `EvaluateAverageByGroup`, `EvaluateDuplicatesByGroup`, `EvaluateUniquesByGroup`, `EvaluateTopByGroup`
- Full write surface: `Add`, `Update`, `Delete`, `AddRange`, `UpdateRange`, `DeleteRange`, `Upsert`, `UpsertRange`, `DeleteByCondition`, `SaveChanges`
- `SetValiFlow` — replace the active filter at runtime
- `InMemoryWriteStore<T>` — extracted write store for testability and SRP compliance
- XML documentation on all public types and members

---

## Vali-Flow.Sql — 1.0.0 *(initial release)*

### Added
- `SqlQueryBuilder<T>` — fluent SELECT builder with JOINs, WHERE, GROUP BY, HAVING, ORDER BY, UNION, EXISTS, CASE WHEN, pagination, query hints
- `SqlInsertBuilder<T>` — INSERT with column mapping and multi-row support
- `SqlUpdateBuilder<T>` — UPDATE with SET clauses and WHERE conditions
- `SqlDeleteBuilder<T>` — DELETE with WHERE conditions
- `SqlTruncateBuilder<T>` — TRUNCATE TABLE
- `SqlMergeBuilder<TTarget, TSource>` — MERGE / UPSERT statement builder
- `SqlWhereBuilder<T>` — standalone WHERE clause builder with fluent AND/OR chaining
- `SqlHavingBuilder<T>` — standalone HAVING clause builder
- `CaseWhenBuilder` — fluent CASE WHEN / THEN / ELSE builder
- `SqlExpressionTranslator` — converts `ValiFlow<T>` expression trees to parameterized SQL
- `SqlResult` — holds the generated SQL string and parameter dictionary; `ApplyTo(IDbCommand)` applies parameters to ADO.NET commands
- `SqlQueryResult` — combines `SqlResult` with pagination metadata
- Dialect support: `SqlServerDialect`, `PostgreSqlDialect`, `MySqlDialect`, `SqliteDialect`, `OracleDialect`
- `ValiFlowSqlExtensions.ToSql(dialect)` — extension method on `ValiFlow<T>`
- XML documentation on all public types and members

---

## Vali-Flow.NoSql — bundled with provider packages

### Vali-Flow.NoSql.MongoDB — 1.0.0 *(initial release)*
- `MongoFilterTranslator` — translates IR nodes to MongoDB `FilterDefinition<T>`
- `ValiFlowMongoExtensions.ToMongoFilter()` — extension on `ValiFlow<T>`

### Vali-Flow.NoSql.DynamoDB — 1.0.0 *(initial release)*
- `DynamoFilterTranslator` — translates IR nodes to DynamoDB `FilterExpression` + `ExpressionAttributeValues`
- `DynamoFilterExpression` — holds the expression string and attribute map
- `ValiFlowDynamoExtensions.ToDynamoFilter()` — extension on `ValiFlow<T>`

### Vali-Flow.NoSql.Elasticsearch — 1.0.0 *(initial release)*
- `ElasticsearchFilterTranslator` — translates IR nodes to Elasticsearch `Query` (Elastic.Clients.Elasticsearch)
- `ValiFlowElasticsearchExtensions.ToElasticsearchQuery()` — extension on `ValiFlow<T>`

### Vali-Flow.NoSql.Redis — 1.0.0 *(initial release)*
- `RedisSearchFilterTranslator` — translates IR nodes to RediSearch query strings
- `ValiFlowRedisSearchExtensions.ToRedisQuery()` — extension on `ValiFlow<T>`

---

## Vali-Flow — 1.0.0 *(initial release)*

### Added
- `ValiFlowEvaluator<T>` — EF Core async evaluator implementing `IEvaluatorRead<T>` and `IEvaluatorWrite<T>`
- `BasicSpecification<T>` and `QuerySpecification<T>` — specification pattern with fluent builder API
- `EvaluateAsync`, `EvaluateAnyAsync`, `EvaluateCountAsync`, `EvaluateGetFirstAsync`, `EvaluateGetLastAsync`, `EvaluateQueryAsync`, `EvaluateQueryFailedAsync`
- `AddAsync`, `UpdateAsync`, `DeleteAsync`, `AddRangeAsync`, `UpdateRangeAsync`, `DeleteRangeAsync`, `UpsertAsync`, `SaveChangesAsync`
- `IEvaluatorRead<T>`, `IEvaluatorWrite<T>`, `ISpecification<T>`, `IBasicSpecification<T>`, `IQuerySpecification<T>`
- `EfInclude<T,TProperty>`, `EfOrderBy<T,TProperty>`, `EfOrderThenBy<T,TProperty>` — EF Core option wrappers
- `PagedResult<T>` — pagination result model
- Dependency on `Vali-Flow.Core` for expression tree building (`ValiFlow<T>`, `ValiSort<T>`)
