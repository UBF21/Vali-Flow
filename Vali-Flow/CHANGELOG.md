# Changelog — Vali-Flow

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.1.0]

### Added
- `EvaluatePagedAsync` — returns `PagedResult<T>` with `Items`, `TotalCount`, `TotalPages`, `HasNextPage`, `HasPreviousPage`
- `EvaluateTopByGroupAsync` — top-N entities per group key
- `EvaluateDistinctAsync` / `EvaluateDuplicatesAsync` — distinct and duplicate detection by key selector
- `EvaluateAggregateAsync` — generic aggregation over any numeric selector
- `EvaluateGroupedAsync`, `EvaluateCountByGroupAsync`, `EvaluateSumByGroupAsync`, `EvaluateMinByGroupAsync`, `EvaluateMaxByGroupAsync`, `EvaluateAverageByGroupAsync`
- `ExecuteUpdateAsync` — bulk in-place update via `ExecuteUpdate` (no entity load)
- `ExecuteTransactionAsync` — wraps multiple operations in a single EF Core transaction
- `BulkInsertAsync`, `BulkUpdateAsync`, `BulkDeleteAsync`, `BulkInsertOrUpdateAsync` — high-throughput bulk operations via `EFCore.BulkExtensions`
- `UpsertRangeAsync` — batch upsert with configurable key selector
- `DeleteByConditionAsync` — delete by raw predicate without loading entities
- `EvaluateGetLastAsync` / `EvaluateGetLastFailedAsync` — last-match retrieval
- `QuerySpecification<T>` — fluent spec builder with `WithTop`, `WithPagination`, `WithOrderBy`, `AddThenBy`, `WithValiSort`
- `BasicSpecification<T>` — simplified spec without ordering/pagination
- XML documentation on all public types and members

### Changed
- `ValiFlowEvaluator<T>` refactored as `sealed partial class` split across focused files (Read, Write, Grouped, Aggregates, Bridge)
- Improved exception messages for invalid spec combinations (Top + Pagination, Pagination without OrderBy)
- `EvaluateAllAsync` and `EvaluateAllFailedAsync` marked `[Obsolete]` — use `EvaluateQueryAsync` / `EvaluateQueryFailedAsync`
- License changed from Apache-2.0 to MIT

### Fixed
- Thread-safety issues in concurrent evaluator usage
- Cache and DRY violations in `UpsertCore` deferred execution
- CS87xx nullable warnings across evaluator and specification classes

---

## [1.0.0] — Initial release

### Added
- `ValiFlowEvaluator<T>` — EF Core async evaluator implementing `IEvaluatorRead<T>` and `IEvaluatorWrite<T>`
- `BasicSpecification<T>` and `QuerySpecification<T>` — specification pattern with fluent builder API
- Read methods: `EvaluateAsync`, `EvaluateAnyAsync`, `EvaluateCountAsync`, `EvaluateGetFirstAsync`, `EvaluateQueryAsync`, `EvaluateQueryFailedAsync`
- Write methods: `AddAsync`, `UpdateAsync`, `DeleteAsync`, `AddRangeAsync`, `UpdateRangeAsync`, `DeleteRangeAsync`, `UpsertAsync`, `SaveChangesAsync`
- Interfaces: `IEvaluatorRead<T>`, `IEvaluatorWrite<T>`, `ISpecification<T>`, `IBasicSpecification<T>`, `IQuerySpecification<T>`
- EF Core option wrappers: `EfInclude<T,TProperty>`, `EfOrderBy<T,TProperty>`, `EfOrderThenBy<T,TProperty>`
- `PagedResult<T>` — pagination result model
- Dependency on `Vali-Flow.Core` for expression tree building (`ValiFlow<T>`, `ValiSort<T>`)
