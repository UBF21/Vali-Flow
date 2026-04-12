# Changelog — Vali-Flow.NoSql

All notable changes to this shared IR layer are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.0.0] — Initial release

### Added
- IR (Intermediate Representation) node types: `EqualNode`, `ComparisonNode`, `AndNode`, `OrNode`, `InNode`, `LikeNode`, `NullNode`
- `IConditionNode` — base interface for all IR nodes
- `IConditionNodeVisitor<TResult>` — visitor interface for IR traversal
- `ComparisonOp` enum — `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`
- `LikeOp` enum — `Contains`, `StartsWith`, `EndsWith`
- `NullCheckOp` enum — `IsNull`, `IsNotNull`
- `UnsupportedNodeDetector` — detects unsupported IR nodes before translation
- `ConditionValueResolver` — resolves and normalizes condition values across adapters
- `ExpressionToIRVisitor` — translates `ValiFlow<T>` expression trees into IR nodes
- `ValiFlowNoSqlExtensions.ToIR()` — extension method on `ValiFlow<T>`
- XML documentation on all public types and members
