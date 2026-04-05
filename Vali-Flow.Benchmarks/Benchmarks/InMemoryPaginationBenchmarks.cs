using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

namespace Vali_Flow.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for InMemory top-N and pagination operations.
/// Baseline: Top100. Paged is a secondary reference.
/// </summary>
[MemoryDiagnoser]
public class InMemoryPaginationBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int N;

    private List<BenchmarkOrder> _data    = null!;
    private readonly Consumer    _consumer = new();

    private ValiFlowEvaluator<BenchmarkOrder, int> _evalIsActive = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkDataFactory.Generate(N);

        var filter = new ValiFlow<BenchmarkOrder>().IsTrue(o => o.IsActive);
        _evalIsActive = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filter, o => o.Id);
    }

    // ── Top N (baseline) ──────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public void Top100_IsActive_Linq() => _data.Where(o => o.IsActive).Take(100).Consume(_consumer);

    [Benchmark]
    public void Top100_IsActive_ValiFlow() => _evalIsActive.EvaluateTop<int>(_data, 100).Consume(_consumer);

    // ── Paged (reference — no ratio) ──────────────────────────────────────────

    [Benchmark]
    public void Page1x50_IsActive_Linq() => _data.Where(o => o.IsActive).Skip(0).Take(50).Consume(_consumer);

    [Benchmark]
    public void Page1x50_IsActive_ValiFlow() => _evalIsActive.EvaluatePaged<int>(_data, 1, 50).Consume(_consumer);
}
