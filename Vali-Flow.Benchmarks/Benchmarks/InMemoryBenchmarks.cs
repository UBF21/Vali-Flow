using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

namespace Vali_Flow.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for InMemory read operations — count, filter, pagination, ordering.
/// Each ValiFlow benchmark is paired with a LINQ baseline to measure overhead.
/// </summary>
[MemoryDiagnoser]
public class InMemoryBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int N;

    private List<BenchmarkOrder> _data = null!;
    private readonly Consumer    _consumer = new();

    // Evaluators for each predicate scenario
    private ValiFlowEvaluator<BenchmarkOrder, int> _evalIsActive  = null!;
    private ValiFlowEvaluator<BenchmarkOrder, int> _evalStatus    = null!;
    private ValiFlowEvaluator<BenchmarkOrder, int> _evalCompound  = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkDataFactory.Generate(N);

        // 50% selectivity
        var filterIsActive = new ValiFlow<BenchmarkOrder>().IsTrue(o => o.IsActive);
        _evalIsActive = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filterIsActive, o => o.Id);

        // ~33% selectivity
        var filterStatus = new ValiFlow<BenchmarkOrder>().EqualTo(o => o.Status, "Completed");
        _evalStatus = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filterStatus, o => o.Id);

        // ~25% selectivity (compound: 3 conditions)
        var filterCompound = new ValiFlow<BenchmarkOrder>()
            .IsTrue(o => o.IsActive)
            .GreaterThan(o => o.Amount, 5000m)
            .GreaterThan(o => o.Quantity, 50);
        _evalCompound = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filterCompound, o => o.Id);
    }

    // ── Count ────────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public int Count_IsActive_Linq() => _data.Count(o => o.IsActive);

    [Benchmark]
    public int Count_IsActive_ValiFlow() => _evalIsActive.EvaluateCount(_data);

    [Benchmark(Baseline = true)]
    public int Count_Compound_Linq() => _data.Count(o => o.IsActive && o.Amount > 5000m && o.Quantity > 50);

    [Benchmark]
    public int Count_Compound_ValiFlow() => _evalCompound.EvaluateCount(_data);

    // ── Filter all ───────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public void FilterAll_IsActive_Linq() => _data.Where(o => o.IsActive).Consume(_consumer);

    [Benchmark]
    public void FilterAll_IsActive_ValiFlow() => _evalIsActive.EvaluateAll<int>(_data).Consume(_consumer);

    [Benchmark(Baseline = true)]
    public void FilterAll_Status_Linq() => _data.Where(o => o.Status == "Completed").Consume(_consumer);

    [Benchmark]
    public void FilterAll_Status_ValiFlow() => _evalStatus.EvaluateAll<int>(_data).Consume(_consumer);

    // ── Any ──────────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public bool Any_IsActive_Linq() => _data.Any(o => o.IsActive);

    [Benchmark]
    public bool Any_IsActive_ValiFlow() => _evalIsActive.EvaluateAny(_data);

    // ── Top N ────────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public void Top100_IsActive_Linq() => _data.Where(o => o.IsActive).Take(100).Consume(_consumer);

    [Benchmark]
    public void Top100_IsActive_ValiFlow() => _evalIsActive.EvaluateTop<int>(_data, 100).Consume(_consumer);

    // ── Pagination ───────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public void Page1x50_IsActive_Linq() => _data.Where(o => o.IsActive).Skip(0).Take(50).Consume(_consumer);

    [Benchmark]
    public void Page1x50_IsActive_ValiFlow() => _evalIsActive.EvaluatePaged<int>(_data, 1, 50).Consume(_consumer);

    // ── GetFirst / GetLast ───────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public BenchmarkOrder? GetFirst_IsActive_Linq() => _data.FirstOrDefault(o => o.IsActive);

    [Benchmark]
    public BenchmarkOrder? GetFirst_IsActive_ValiFlow() => _evalIsActive.GetFirst(_data);

    [Benchmark(Baseline = true)]
    public BenchmarkOrder? GetLast_IsActive_Linq() => _data.LastOrDefault(o => o.IsActive);

    [Benchmark]
    public BenchmarkOrder? GetLast_IsActive_ValiFlow() => _evalIsActive.GetLast<int>(_data);
}
