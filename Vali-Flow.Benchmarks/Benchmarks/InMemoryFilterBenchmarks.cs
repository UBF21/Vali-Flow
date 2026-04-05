using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

namespace Vali_Flow.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for InMemory filter (enumerate all), Any operations.
/// Baseline: IsActive filter-all. Status and Any are secondary references.
/// </summary>
[MemoryDiagnoser]
public class InMemoryFilterBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int N;

    private List<BenchmarkOrder> _data    = null!;
    private readonly Consumer    _consumer = new();

    private ValiFlowEvaluator<BenchmarkOrder, int> _evalIsActive = null!;
    private ValiFlowEvaluator<BenchmarkOrder, int> _evalStatus   = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkDataFactory.Generate(N);

        var filterIsActive = new ValiFlow<BenchmarkOrder>().IsTrue(o => o.IsActive);
        _evalIsActive = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filterIsActive, o => o.Id);

        var filterStatus = new ValiFlow<BenchmarkOrder>().EqualTo(o => o.Status, "Completed");
        _evalStatus = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filterStatus, o => o.Id);
    }

    // ── Filter all (baseline) ─────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public void FilterAll_IsActive_Linq() => _data.Where(o => o.IsActive).Consume(_consumer);

    [Benchmark]
    public void FilterAll_IsActive_ValiFlow() => _evalIsActive.EvaluateAll<int>(_data).Consume(_consumer);

    // ── Filter by status (reference — no ratio) ───────────────────────────────

    [Benchmark]
    public void FilterAll_Status_Linq() => _data.Where(o => o.Status == "Completed").Consume(_consumer);

    [Benchmark]
    public void FilterAll_Status_ValiFlow() => _evalStatus.EvaluateAll<int>(_data).Consume(_consumer);

    // ── Any (reference — no ratio) ────────────────────────────────────────────

    [Benchmark]
    public bool Any_IsActive_Linq() => _data.Any(o => o.IsActive);

    [Benchmark]
    public bool Any_IsActive_ValiFlow() => _evalIsActive.EvaluateAny(_data);
}
