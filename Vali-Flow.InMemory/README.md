# Vali-Flow.InMemory

Synchronous, dependency-free evaluator that operates on `IEnumerable<T>` — built on top of `Vali-Flow.Core`. No database required. Ideal for unit testing, in-process caching layers, and scenarios where data lives in memory.

## Install

```bash
dotnet add package Vali-Flow.InMemory
```

## Quick Start

```csharp
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

// TProperty is the type of the entity's identity key
var evaluator = new ValiFlowEvaluator<Order, Guid>(
    initialData: orders,
    valiFlow: null,
    getId: x => x.Id);

var filter = new ValiFlow<Order>().EqualTo(x => x.Status, "Active");

bool   any   = evaluator.EvaluateAny(orders, filter);
int    count = evaluator.EvaluateCount(orders, filter);
Order? first = evaluator.GetFirst(orders, filter);
```

## Reading Data

### Existence and count

```csharp
bool any   = evaluator.EvaluateAny(orders, filter);
int  count = evaluator.EvaluateCount(orders, filter);
```

### Single entity

```csharp
Order? first = evaluator.GetFirst(orders, filter);
Order? last  = evaluator.GetLast(orders, filter);

// Entities that do NOT match the filter
Order? firstFailed = evaluator.GetFirstFailed(orders, filter);
Order? lastFailed  = evaluator.GetLastFailed(orders, filter);
```

### Ordered list

```csharp
IEnumerable<Order> all = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    ascending: false,
    valiFlow: filter);
```

### Pagination

```csharp
IEnumerable<Order> paged = evaluator.EvaluatePaged<DateTime>(
    orders,
    page: 1,
    pageSize: 10,
    orderBy: x => x.CreatedAt,
    ascending: false,
    valiFlow: filter);
```

### Top-N

```csharp
IEnumerable<Order> top5 = evaluator.EvaluateTop<decimal>(
    orders,
    count: 5,
    orderBy: x => x.Total,
    ascending: false,
    valiFlow: filter);
```

### Distinct and duplicates

```csharp
IEnumerable<Order> distinct = evaluator.EvaluateDistinct<Guid>(
    orders,
    selector: x => x.CustomerId,
    orderBy: x => x.CreatedAt,
    ascending: false,
    valiFlow: filter);

IEnumerable<Order> duplicates = evaluator.EvaluateDuplicates<Guid>(
    orders,
    selector: x => x.CustomerId,
    valiFlow: filter);
```

## Aggregates

```csharp
decimal min = evaluator.EvaluateMin(orders, x => x.Total, filter);
decimal max = evaluator.EvaluateMax(orders, x => x.Total, filter);
decimal avg = evaluator.EvaluateAverage(orders, x => x.Total, filter);
decimal sum = evaluator.EvaluateSum(orders, x => x.Total, filter);
```

## Grouped Operations

```csharp
Dictionary<string, List<Order>> grouped =
    evaluator.EvaluateGrouped(orders, x => x.Status, filter);

Dictionary<string, int> countByStatus =
    evaluator.EvaluateCountByGroup(orders, x => x.Status, filter);

Dictionary<string, decimal> sumByStatus =
    evaluator.EvaluateSumByGroup(orders, x => x.Status, x => x.Total, filter);

Dictionary<string, decimal> avgByStatus =
    evaluator.EvaluateAverageByGroup(orders, x => x.Status, x => x.Total, filter);

Dictionary<string, decimal> minByStatus =
    evaluator.EvaluateMinByGroup(orders, x => x.Status, x => x.Total, filter);

Dictionary<string, decimal> maxByStatus =
    evaluator.EvaluateMaxByGroup(orders, x => x.Status, x => x.Total, filter);

Dictionary<string, List<Order>> topByStatus =
    evaluator.EvaluateTopByGroup(orders, x => x.Status, count: 3, orderBy: x => x.Total, filter);
```

## Writing Data

```csharp
evaluator.Add(newOrder, orders);
evaluator.Update(existingOrder, orders);
evaluator.Delete(existingOrder, orders);

evaluator.AddRange(newOrders, orders);
IEnumerable<Order> updated = evaluator.UpdateRange(modifiedOrders, orders);
int deleted = evaluator.DeleteRange(staleOrders, orders);

evaluator.SaveChanges(orders);
```

### Upsert

```csharp
// Insert if no match found, update otherwise
evaluator.Upsert(order, orders);

// Collection — matched by identity key
evaluator.UpsertRange(newOrders, orders);
```

## Negating Conditions

Every read method accepts a `negateCondition` parameter. When `true`, the filter is inverted — equivalent to a logical NOT.

```csharp
// All orders that do NOT have Status == "Active"
IEnumerable<Order> inactive = evaluator.EvaluateAll<DateTime>(
    orders,
    orderBy: x => x.CreatedAt,
    valiFlow: new ValiFlow<Order>().EqualTo(x => x.Status, "Active"),
    negateCondition: true);
```

## What it supports

| Feature | Supported |
|---------|-----------|
| Filter (`ValiFlow<T>`) | Yes |
| Ordering (primary + secondary) | Yes |
| Pagination (page + pageSize) | Yes |
| Top-N | Yes |
| Aggregates (Min/Max/Avg/Sum) | Yes |
| Group-by aggregates | Yes |
| Distinct / Duplicates | Yes |
| CRUD + UpsertRange | Yes |
| Negate condition | Yes |
| Async / DbContext | No — by design |

## Full documentation

[vali-flow-docs.netlify.app/docs/adapters/inmemory](https://vali-flow-docs.netlify.app/docs/adapters/inmemory)

## Contributing

Contributions, issues, and feature requests are welcome. Feel free to open a pull request or an issue on [GitHub](https://github.com/UBF21/vali-flow).

If this package is useful to you, consider supporting its development:

- **Latin America** — [MercadoPago](https://link.mercadopago.com.pe/felipermm)
- **International** — [PayPal](https://paypal.me/felipeRMM?country.x=PE&locale.x=es_XC)

## License

Licensed under the [MIT License](LICENSE).  
Copyright &copy; 2025 Felipe Rafael Montenegro Morriberon. All rights reserved.
