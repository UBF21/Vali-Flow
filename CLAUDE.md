# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Vali-Flow** is a .NET library ecosystem that simplifies data access in Entity Framework Core applications via a fluent specification API. It is distributed as three independent NuGet packages, all built on top of **Vali-Flow.Core** (the expression tree builder — maintained in a separate repository at `/Users/feliperafaelmontenegro/RiderProjects/Vali-Flow.Core`).

| Package | Version | Purpose |
|---------|---------|---------|
| `Vali-Flow` | 1.1.0 | EF Core evaluator + specification classes |
| `Vali-Flow.InMemory` | 1.0.0 | Synchronous in-memory evaluator (testing/caching) |
| `Vali-Flow.Sql` | 1.0.0 | Expression → parameterized SQL translator (Dapper/ADO.NET) |

## Vali-Flow.Core Dependency

All packages depend on **Vali-Flow.Core**, which provides `ValiFlow<T>` (the fluent expression builder) and `ValiSort<T>`.

- All three packages (`Vali-Flow`, `Vali-Flow.InMemory`, `Vali-Flow.Sql`) reference Core via local `ProjectReference` pointing to `/Users/feliperafaelmontenegro/RiderProjects/Vali-Flow.Core/Vali-Flow.Core/Vali-Flow.Core.csproj`.
- **Do not change these to NuGet references yet** — Core v2.6.0 has not been published to NuGet. Once deployed, replace each `ProjectReference` with `<PackageReference Include="Vali-Flow.Core" Version="2.6.0" />`.
- **Current local Core version is 2.6.0** (net8.0 + net9.0, 911 tests). Core was extensively refactored: bugs fixed, new methods added (IsInEnum, IsDefault, IsCreditCard, IsIPv4/IPv6, IsHexColor, IsSlug, WithMessage(Func<string>), IsLastDayOfMonth, EF Core-safe DateOnly/DateTimeOffset methods).

## Build & Pack Commands

```bash
# Build the full solution
dotnet build

# Build in release
dotnet build --configuration Release

# Pack individual packages
dotnet pack Vali-Flow/Vali-Flow.csproj --configuration Release
dotnet pack Vali-Flow.InMemory/Vali-Flow.InMemory.csproj --configuration Release
dotnet pack Vali-Flow.Sql/Vali-Flow.Sql.csproj --configuration Release
```

> There are currently no automated tests in this repo. The `vali-flow-test/` project is a console demo app, not a test suite.

## Architecture

### Package Structure

```
Vali-Flow.Core  (local — /Users/feliperafaelmontenegro/RiderProjects/Vali-Flow.Core)
       ↓  (ProjectReference — all three packages)
┌──────┴───────────────────────────────┐
│  Vali-Flow (EF Core, async)          │
│  Vali-Flow.InMemory (sync)           │
│  Vali-Flow.Sql (Dapper/ADO.NET)      │
└──────────────────────────────────────┘
```

### Vali-Flow (Main Package)

**Entry point:** `ValiFlowEvaluator<T>` — a sealed class taking a `DbContext`. Implements `IEvaluatorRead<T>` + `IEvaluatorWrite<T>`.

**Specifications:**
```
ISpecification<T>
  └─ IBasicSpecification<T> → BasicSpecification<T>   (filter + includes + EF options)
       └─ IQuerySpecification<T> → QuerySpecification<T>   (+ ordering + pagination + top)
```

`BasicSpecification<T>` holds: `ValiFlow<T>` filter, `List<IEfInclude<T>>` includes, and EF Core hints (AsNoTracking, AsSplitQuery, IgnoreQueryFilters).

`QuerySpecification<T>` adds: primary `IEfOrderBy<T>`, secondary `List<IEfOrderThenBy<T>>`, page/pageSize/top, and `ValiSort<T>`.

**Options** (in `Classes/Options/`): `EfInclude<T,TProperty>`, `EfOrderBy<T,TProperty>`, `EfOrderThenBy<T,TProperty>` — all sealed, generic over the property type.

**Evaluator read methods:** EvaluateAsync (single entity), EvaluateAnyAsync, EvaluateCountAsync, EvaluateGetFirstAsync, EvaluateGetLastAsync, EvaluateGetFirstFailedAsync, EvaluateGetLastFailedAsync, EvaluateQueryAsync, EvaluateQueryFailedAsync, EvaluateDistinctAsync, EvaluateDuplicatesAsync, aggregate methods (Min/Max/Average/Sum), grouped methods (EvaluateGroupedAsync, EvaluateCountByGroupAsync, EvaluateSumByGroupAsync, EvaluateMinByGroupAsync, EvaluateMaxByGroupAsync, EvaluateAverageByGroupAsync, EvaluateTopByGroupAsync, EvaluateAggregateAsync).

**Evaluator write methods:** AddAsync, UpdateAsync, DeleteAsync, AddRangeAsync, UpdateRangeAsync, DeleteRangeAsync, DeleteByConditionAsync, UpsertAsync, UpsertRangeAsync, BulkInsertAsync, BulkUpdateAsync, BulkDeleteAsync, BulkInsertOrUpdateAsync, ExecuteTransactionAsync, SaveChangesAsync.

### Vali-Flow.InMemory

**Entry point:** `ValiFlowEvaluator<T, TProperty>` — synchronous, operates on `IEnumerable<T>`. Takes `initialData`, an optional `ValiFlow<T>`, and a `Func<T, TProperty> getId`. Extends `EvaluatorBase<T, TProperty>`.

Mirrors the EF Core evaluator API but synchronous and without a DbContext. Useful for unit testing and caching layers.

### Vali-Flow.Sql

**Entry point:** `ValiFlowSqlExtensions` — provides `.ToSql(dialect)` extension method on `ValiFlow<T>`, returning a `SqlResult` (with `Sql` string and `IReadOnlyDictionary<string, object> Parameters`).

**Translator:** `ExpressionToSqlVisitor` (sealed, internal) — an `ExpressionVisitor` that walks expression trees and emits dialect-specific SQL.

**Dialects (all sealed):**
- `SqlServerDialect` — `@param`, `[col]`, `LOWER()`
- `PostgreSqlDialect` — `@param`, `"col"`, native `ILIKE`
- `MySqlDialect` — `@param`, `` `col` ``, case-insensitive `LIKE`
- `SqliteDialect` — `@param`, `"col"`, `LOWER()`

`SqlResult.ApplyTo(IDbCommand)` applies parameters directly to an ADO.NET command.

## Key Design Decisions

- **Specification pattern**: query criteria are decoupled from the evaluator — build a spec, pass it to the evaluator.
- **Deferred saves**: pass `saveChanges: false` to batch operations and call `SaveChangesAsync()` once.
- **Bulk operations**: `BulkInsert/Update/Delete/InsertOrUpdate` delegate to `EFCore.BulkExtensions` — pass a `BulkConfig` to control batch size and identity output.
- **`negateCondition` parameter**: all read methods accept this flag to invert the `ValiFlow<T>` filter (logical NOT).
- **`ValiSort<T>`**: sorting helper from Vali-Flow.Core; used by `QuerySpecification` for dynamic/reflection-based ordering.
- **No xUnit tests yet**: testing is done via the `vali-flow-test` console app. A proper test project (xUnit + FluentAssertions) should be added following the same pattern as `Vali-Flow.Core.Tests`.
