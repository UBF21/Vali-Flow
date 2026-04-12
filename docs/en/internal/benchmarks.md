# Vali-Flow Benchmarks

The `Vali-Flow.Benchmarks` project measures the performance of the InMemory evaluator and the SQL builder using [BenchmarkDotNet](https://benchmarkdotnet.org/).

---

## Running benchmarks

Always run in Release mode. Debug builds disable BenchmarkDotNet's warmup and measurement phases.

```bash
cd Vali-Flow.Benchmarks
dotnet run --configuration Release
```

Results land in `BenchmarkDotNet.Artifacts/results/`. Do not commit these files — they are gitignored.

To run a specific benchmark class:

```bash
dotnet run --configuration Release -- --filter "*InMemoryFilter*"
dotnet run --configuration Release -- --filter "*SqlBuilder*"
```

---

## Benchmark suites

### InMemory evaluator

All InMemory benchmarks parameterize over `N ∈ { 1 000, 10 000, 100 000 }` to show how each operation scales.

| Class | What it measures |
|-------|-----------------|
| `InMemoryFilterBenchmarks` | `EvaluateQuery` (enumerate all) and `EvaluateAny` — baseline is raw LINQ `.Where()` |
| `InMemoryCountBenchmarks` | `EvaluateCount` with a simple filter vs a compound filter |
| `InMemoryAggregateBenchmarks` | `EvaluateSum`, `EvaluateAverage`, `EvaluateMin`, `EvaluateMax`, `EvaluateCountByGroup` |
| `InMemoryWriteBenchmarks` | `Add`, `Update`, `Delete`, `Upsert` — per-operation cost |
| `InMemoryPaginationBenchmarks` | `EvaluatePaged` at different page sizes |
| `InMemoryLookupBenchmarks` | Direct entity lookup by ID vs filtered search |

### SQL builder

| Class | What it measures |
|-------|-----------------|
| `SqlBuilderBenchmarks` | `SqlQueryBuilder` SELECT with JOINs, INSERT, UPDATE — construction cost only (no DB round-trip) |

---

## Interpreting results

BenchmarkDotNet reports:

| Column | Meaning |
|--------|---------|
| `Mean` | Average time per operation across all iterations |
| `Ratio` | Relative to the marked `[Benchmark(Baseline = true)]` method |
| `Allocated` | Managed heap allocated per operation (requires `[MemoryDiagnoser]`) |

**What to look for:**
- `Ratio ≈ 1.0` — on par with the baseline (typically raw LINQ)
- `Ratio < 2.0` — acceptable overhead for the evaluator abstraction layer
- Allocated bytes growing with `N` — expected; flat allocation across `N` sizes indicates a potential issue

---

## Adding a new benchmark

1. Create a class in `Vali-Flow.Benchmarks/Benchmarks/` annotated with `[MemoryDiagnoser]`.
2. Parameterize with `[Params(1_000, 10_000, 100_000)]` for collection-size-sensitive operations.
3. Mark one method as `[Benchmark(Baseline = true)]` — use the equivalent raw LINQ/direct operation as the baseline.
4. Use `BenchmarkDataFactory.Generate(N)` to produce consistent, reproducible test data.
5. Avoid side effects in benchmarks — write results to a `Consumer` (BenchmarkDotNet's sink), not to `Console`.

---

## Known limitations

- **No NoSQL benchmarks** — `Vali-Flow.NoSql.*` translators are not benchmarked. Translation is CPU-only (no I/O) and the trees are small, so overhead is expected to be sub-microsecond.
- **No EF Core benchmarks** — EF Core performance is dominated by the database round-trip, not the Vali-Flow layer. Profiling should be done with actual query plans, not microbenchmarks.

---

## See also

- [Contributing guide](contributing.md)
- [Architecture overview](../architecture/overview.md)
