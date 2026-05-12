# Vali-Flow.Abstractions

**Provider-agnostic contracts and utilities** for the Vali-Flow ecosystem.

This package contains the shared interfaces and helper classes that all Vali-Flow adapters (EF Core, In-Memory, SQL, MongoDB, Elasticsearch, Redis, DynamoDB, and future providers) implement or use.

## Contents

- **IQueryEvaluator<T>** — Base contract combining IQueryReader<T> and IQueryAggregator<T>
- **IQueryReader<T>** — Async read operations (count, exists, get first/last, etc.)
- **IQueryAggregator<T>** — Async aggregation operations (min, max, sum, average)
- **IExpressionTranslator<TOutput>** — Expression tree to provider-specific format translator
- **ExpressionInspector** — Shared expression-tree inspection utilities

## Usage

This package is **internal to the Vali-Flow ecosystem**. End users should not depend on it directly. Instead, use:

- **Vali-Flow** for Entity Framework Core
- **Vali-Flow.InMemory** for in-memory testing/caching
- **Vali-Flow.Sql** for parameterized SQL (Dapper/ADO.NET)
- **Vali-Flow.NoSql.*** for NoSQL providers (MongoDB, Elasticsearch, Redis, DynamoDB)

## Installation

```bash
dotnet add package Vali-Flow.Abstractions
```

All main packages (`Vali-Flow`, `Vali-Flow.InMemory`, `Vali-Flow.Sql`) automatically include Vali-Flow.Abstractions as a transitive dependency.

---

Licensed under the [MIT License](https://github.com/UBF21/vali-flow/blob/main/LICENSE).
