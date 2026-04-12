# Vali-Flow.InMemory

Synchronous in-memory evaluator for testing, caching, and offline evaluation against `IEnumerable<T>`.

See the [main README](../README.md) for complete documentation and examples.

## Installation

```bash
dotnet add package Vali-Flow.InMemory
```

## Quick Example

```csharp
using Vali_Flow.InMemory;

var evaluator = new ValiFlowEvaluator<Order, int>(orders, null, x => x.Id);

var filter = new ValiFlow<Order>()
    .EqualTo(x => x.Status, "Active")
    .GreaterThan(x => x.Total, 100m);

int count = evaluator.EvaluateCount(orders, filter);
var filtered = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    valiFlow: filter);
```

## Features

- Synchronous evaluation without database
- Read operations: filtering, sorting, pagination, aggregates
- Grouped operations: `EvaluateGrouped`, `EvaluateCountByGroup`, `EvaluateSumByGroup`
- Write operations: `Add`, `Update`, `Delete`, `SaveChanges`
- No external dependencies

## License

MIT
