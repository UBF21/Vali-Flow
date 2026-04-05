using BenchmarkDotNet.Attributes;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

namespace Vali_Flow.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for InMemory count operations.
/// Baseline: simple IsActive filter. Compound filter is a secondary reference (no ratio).
/// </summary>
[MemoryDiagnoser]
public class InMemoryCountBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int N;

    private List<BenchmarkOrder> _data = null!;

    private ValiFlowEvaluator<BenchmarkOrder, int> _evalIsActive = null!;
    private ValiFlowEvaluator<BenchmarkOrder, int> _evalCompound = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkDataFactory.Generate(N);

        var filterIsActive = new ValiFlow<BenchmarkOrder>().IsTrue(o => o.IsActive);
        _evalIsActive = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filterIsActive, o => o.Id);

        var filterCompound = new ValiFlow<BenchmarkOrder>()
            .IsTrue(o => o.IsActive)
            .GreaterThan(o => o.Amount, 5000m)
            .GreaterThan(o => o.Quantity, 50);
        _evalCompound = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filterCompound, o => o.Id);
    }

    // ── Simple filter ─────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public int Count_IsActive_Linq() => _data.Count(o => o.IsActive);

    [Benchmark]
    public int Count_IsActive_ValiFlow() => _evalIsActive.EvaluateCount(_data);

    // ── Compound filter (reference — no ratio) ────────────────────────────────

    [Benchmark]
    public int Count_Compound_Linq() => _data.Count(o => o.IsActive && o.Amount > 5000m && o.Quantity > 50);

    [Benchmark]
    public int Count_Compound_ValiFlow() => _evalCompound.EvaluateCount(_data);
}
