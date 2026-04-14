# Changelog

All notable changes to the Vali-Flow ecosystem are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

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
