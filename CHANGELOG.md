# Changelog

All notable changes to the Vali-Flow ecosystem are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added

- **`Vali-Flow.NoSql.Couchbase`, `Vali-Flow.NoSql.CosmosDb`, `Vali-Flow.NoSql.Firestore`** — three new NoSQL translator packages, same pattern as the existing MongoDB/DynamoDB/Elasticsearch/Redis adapters (pure `ValiFlow<T>` expression tree → native query/filter translators, no connection or execution concerns).
- **`GenericRepository<T, TKey>`** (`Vali-Flow`) — a thin repository wrapping `ValiFlowEvaluator<T>` with `GetById`/`GetAll`/`GetPaged`/`Add`/`Update`/`Delete`/`SaveChanges`, for consumers who want a conventional repository surface instead of calling the evaluator directly.
- **`ValiFlowDiagnostics`** (`Vali-Flow.Abstractions`) — shared `ActivitySource`-based `StartActivity`/`RecordException` helper for OpenTelemetry-compatible tracing, now wired through the EF Core evaluator, `Vali-Flow.InMemory`, `Vali-Flow.Sql`'s six builders, and the Mongo/DynamoDB/Elasticsearch/Redis translators.
- EF Core benchmark using the `EFCore.InMemory` provider (`Vali-Flow.Benchmarks`).
- `SqlInsertBuilder`/`SqlUpdateBuilder.SetAllFrom(entity, exclude...)` — reflection-based bulk column mapper.

### Fixed

- **Security — SQL identifier injection**: `Vali-Flow.Sql`'s builders interpolated table/schema/type names directly; added `SqlIdentifierGuard` (regex whitelist) validating all identifiers before interpolation.
- **Security — RediSearch query injection**: `Vali-Flow.NoSql.Redis` didn't escape RediSearch query-syntax special characters (e.g. `)`/`|`) in `LIKE` patterns before wrapping them in wildcards, allowing query-structure injection. Now escaped before wildcarding.
- **`Vali-Flow.NoSql` (Mongo/Elasticsearch)**: closure-captured `null` values in a `ValiFlow<T>` condition weren't translated to the same `NullNode` as a literal `null`, producing inconsistent filters between `.EqualTo(x => x.Prop, someNullVariable)` and `.EqualTo(x => x.Prop, null)`.
- **`Vali-Flow.NoSql.Redis`**: `customConverter` wasn't applied in `VisitComparison`/numeric `VisitIn`, silently ignoring custom type mappings for comparison and `IN` conditions.
- **`Vali-Flow.NoSql.Elasticsearch`**: swallowed inner exception in `ToDouble` conversion — failures surfaced as a generic error with no root cause.
- **`Vali-Flow.NoSql` (Mongo/Elasticsearch)**: unbounded `IN` value lists could exceed the engine's own limits; added `MaxInValues` caps (Mongo 10,000; Elasticsearch 65,536, matching `index.max_terms_count`).
- **`Vali-Flow.NoSql.Couchbase`**: `decimal` values were bound as N1QL string literals instead of native numbers.
- **`Vali-Flow.Sql`**: `OrIgnore`/`OrReplace` dialect guard bug, `Set()`-after-`SelectFrom()` silent data loss, `Where()` double-call silently overwriting the first condition instead of combining — all fixed. Added `AllowDeleteUnmatched()` guard required before `WhenNotMatchedBySourceDelete` in `SqlMergeBuilder` (prevents accidental unguarded deletes in a MERGE).
- **`Vali-Flow` (EF Core evaluator)**: `UpsertRangeAsync` dropped entries on duplicate keys (list accumulator instead of dictionary) — fixed to a dictionary accumulator. `UpsertRangeAsync.PartitionUpsertRange` could discard a tracked entity's primary key — now preserved. `BulkInsertOrUpdateAsync` could race under concurrent calls for the same entity type + key columns — calls are now serialized per that combination.
- **`Vali-Flow` (EF Core evaluator) — bulk upsert contention under concurrency**: replaced the ad-hoc retry with a generic transient-retry policy (`BulkUpsertRetryPolicy`) covering SQL Server, PostgreSQL, MySQL, and SQLite, gated per table (not per `UpdateByProperties` key set) to avoid serializing unrelated upserts against each other. Retry budget raised in two steps (4 → 7 → 12 attempts) based on observed contention under load.
- **`Vali-Flow.InMemory`**: `Update(entity, externalList)` could leak the updated entity into the internal store on a later bare `SaveChanges()` call.

### Changed

- **`Vali-Flow.InMemory`**: `ValiFlowEvaluator<T, TProperty>` (904 lines) split into `Bridge`/`Read`/`Write`/`Grouped` partial files; no public API change.
- **`Vali-Flow` (EF Core evaluator)**: query-construction logic extracted from `ValiFlowEvaluator` into a dedicated `QuerySpecificationBuilder` (single-responsibility split, no public API change).
- EF Core evaluator test coverage raised from 74.37% to 97.42%.
- Eliminated flaky `Activity`-capture tests across the `Sql`/EF-Core/`InMemory` test assemblies (timing-dependent assertions replaced with deterministic capture).

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
