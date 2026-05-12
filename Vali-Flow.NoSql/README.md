# Vali-Flow.NoSql

**Provider-agnostic NoSQL intermediate representation (IR)** for the Vali-Flow ecosystem.

This package provides a neutral condition node tree that translates Vali-Flow.Core expressions into a dialect-agnostic format. Concrete providers (MongoDB, Elasticsearch, Redis, DynamoDB) depend on this package to convert the IR to their native query languages.

## Contents

- **Condition Node Types**: `IConditionNode` and implementations (AND, OR, NOT, =, >, <, IN, LIKE, NULL, etc.)
- **Node Visitor Pattern**: `IConditionNodeVisitor<T>` for traversing and transforming the IR
- **Expression Translator**: Converts `Expression<Func<T, bool>>` to the neutral condition node tree

## Usage

This package is **internal to the Vali-Flow ecosystem**. End users should use the specific provider packages:

- **Vali-Flow.NoSql.MongoDB** for MongoDB BSON filters
- **Vali-Flow.NoSql.Elasticsearch** for Elasticsearch Query DSL
- **Vali-Flow.NoSql.Redis** for RediSearch queries
- **Vali-Flow.NoSql.DynamoDB** for AWS DynamoDB filter expressions

## Installation

```bash
dotnet add package Vali-Flow.NoSql
```

All NoSQL provider packages automatically include Vali-Flow.NoSql as a transitive dependency.

---

Licensed under the [MIT License](https://github.com/UBF21/vali-flow/blob/main/LICENSE).
