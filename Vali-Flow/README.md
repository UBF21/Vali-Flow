# Vali-Flow

Entity Framework Core async evaluator with fluent specifications for read/write operations, bulk operations, and aggregates.

See the [main README](../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow
```

## Quick Example

```csharp
using Vali_Flow;

var evaluator = new ValiFlowEvaluator<Order>(dbContext);

var spec = new BasicSpecification<Order>()
    .WithFilter(new ValiFlow<Order>()
        .EqualTo(x => x.Status, "Active")
        .GreaterThan(x => x.Total, 100m))
    .AddInclude(x => x.Customer)
    .WithAsNoTracking(true);

var orders = await evaluator.EvaluateQueryAsync(spec);
```

## Features

- Fluent `BasicSpecification<T>` and `QuerySpecification<T>` for query criteria
- Read operations: `EvaluateAnyAsync`, `EvaluateCountAsync`, `EvaluateGetFirstAsync`, aggregates
- Write operations: `AddAsync`, `UpdateAsync`, `DeleteAsync`, bulk operations via `EFCore.BulkExtensions`
- Upsert support: `UpsertAsync`, `UpsertRangeAsync`
- Transactions: `ExecuteTransactionAsync`
- Eager loading: `AddInclude`
- EF Core hints: `AsNoTracking`, `AsSplitQuery`, `IgnoreQueryFilters`

## License

MIT
