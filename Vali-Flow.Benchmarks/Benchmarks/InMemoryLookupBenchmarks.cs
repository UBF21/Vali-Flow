using BenchmarkDotNet.Attributes;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

namespace Vali_Flow.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for InMemory first/last lookup operations.
/// Baseline: GetFirst. GetLast is a secondary reference.
/// </summary>
[MemoryDiagnoser]
public class InMemoryLookupBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int N;

    private List<BenchmarkOrder> _data = null!;

    private ValiFlowEvaluator<BenchmarkOrder, int> _evalIsActive = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkDataFactory.Generate(N);

        var filter = new ValiFlow<BenchmarkOrder>().IsTrue(o => o.IsActive);
        _evalIsActive = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filter, o => o.Id);
    }

    // ── GetFirst (baseline) ───────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public BenchmarkOrder? GetFirst_IsActive_Linq() => _data.FirstOrDefault(o => o.IsActive);

    [Benchmark]
    public BenchmarkOrder? GetFirst_IsActive_ValiFlow() => _evalIsActive.GetFirst(_data);

    // ── GetLast (reference — no ratio) ────────────────────────────────────────

    [Benchmark]
    public BenchmarkOrder? GetLast_IsActive_Linq() => _data.LastOrDefault(o => o.IsActive);

    [Benchmark]
    public BenchmarkOrder? GetLast_IsActive_ValiFlow() => _evalIsActive.GetLast<int>(_data);
}
