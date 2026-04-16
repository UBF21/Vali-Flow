# Changelog — Vali-Flow.Sql

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.1.1] - 2026-04-15

### Fixed
- Updated Vali-Flow.Core dependency to 2.0.2 which fixes IsNull/NotNull WHERE 0=1 bug with EF Core GlobalQueryFilter

---

## [1.0.0] — Initial release

### Added
- `SqlQueryBuilder<T>` — fluent SELECT builder with JOINs, WHERE, GROUP BY, HAVING, ORDER BY, UNION, EXISTS, CASE WHEN, pagination, and query hints
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
- Dialects: `SqlServerDialect`, `PostgreSqlDialect`, `MySqlDialect`, `SqliteDialect`, `OracleDialect`
- `ValiFlowSqlExtensions.ToSql(dialect)` — extension method on `ValiFlow<T>`
- `NullsOrder` enum — controls `NULLS FIRST` / `NULLS LAST` in ORDER BY clauses
- XML documentation on all public types and members
