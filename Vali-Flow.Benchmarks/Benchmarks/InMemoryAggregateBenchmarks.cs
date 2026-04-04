using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

namespace Vali_Flow.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for InMemory aggregate operations: Min, Max, Sum, Average and grouped variants.
/// Each ValiFlow benchmark is paired with a LINQ baseline.
/// </summary>
[MemoryDiagnoser]
public class InMemoryAggregateBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int N;

    private List<BenchmarkOrder> _data     = null!;
    private readonly Consumer    _consumer = new();

    private ValiFlowEvaluator<BenchmarkOrder, int> _evalIsActive = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkDataFactory.Generate(N);

        var filter = new ValiFlow<BenchmarkOrder>().IsTrue(o => o.IsActive);
        _evalIsActive = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filter, o => o.Id);
    }

    // ── Sum ──────────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public decimal Sum_Amount_Linq() => _data.Where(o => o.IsActive).Sum(o => o.Amount);

    [Benchmark]
    public decimal Sum_Amount_ValiFlow() => _evalIsActive.EvaluateSum(_data, o => o.Amount);

    [Benchmark(Baseline = true)]
    public int Sum_Quantity_Linq() => _data.Where(o => o.IsActive).Sum(o => o.Quantity);

    [Benchmark]
    public int Sum_Quantity_ValiFlow() => _evalIsActive.EvaluateSum(_data, o => o.Quantity);

    // ── Average ──────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public decimal Average_Amount_Linq() => _data.Where(o => o.IsActive).Average(o => o.Amount);

    [Benchmark]
    public decimal Average_Amount_ValiFlow() => _evalIsActive.EvaluateAverage(_data, o => o.Amount);

    // ── Min / Max ─────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public decimal Min_Amount_Linq() => _data.Where(o => o.IsActive).Min(o => o.Amount);

    [Benchmark]
    public decimal Min_Amount_ValiFlow() => _evalIsActive.EvaluateMin(_data, o => o.Amount);

    [Benchmark(Baseline = true)]
    public decimal Max_Amount_Linq() => _data.Where(o => o.IsActive).Max(o => o.Amount);

    [Benchmark]
    public decimal Max_Amount_ValiFlow() => _evalIsActive.EvaluateMax(_data, o => o.Amount);

    [Benchmark(Baseline = true)]
    public int Min_Quantity_Linq() => _data.Where(o => o.IsActive).Min(o => o.Quantity);

    [Benchmark]
    public int Min_Quantity_ValiFlow() => _evalIsActive.EvaluateMin(_data, o => o.Quantity);

    [Benchmark(Baseline = true)]
    public int Max_Quantity_Linq() => _data.Where(o => o.IsActive).Max(o => o.Quantity);

    [Benchmark]
    public int Max_Quantity_ValiFlow() => _evalIsActive.EvaluateMax(_data, o => o.Quantity);

    // ── Grouped ───────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public Dictionary<string, List<BenchmarkOrder>> GroupedByStatus_Linq() =>
        _data.Where(o => o.IsActive)
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.ToList());

    [Benchmark]
    public Dictionary<string, List<BenchmarkOrder>> GroupedByStatus_ValiFlow() =>
        _evalIsActive.EvaluateGrouped(_data, o => o.Status);

    [Benchmark(Baseline = true)]
    public Dictionary<bool, List<BenchmarkOrder>> GroupedByBool_Linq() =>
        _data.GroupBy(o => o.IsActive).ToDictionary(g => g.Key, g => g.ToList());

    [Benchmark]
    public Dictionary<bool, List<BenchmarkOrder>> GroupedByBool_ValiFlow() =>
        _evalIsActive.EvaluateGrouped(_data, o => o.IsActive);

    // ── CountByGroup ──────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public Dictionary<string, int> CountByStatus_Linq() =>
        _data.Where(o => o.IsActive)
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());

    [Benchmark]
    public Dictionary<string, int> CountByStatus_ValiFlow() =>
        _evalIsActive.EvaluateCountByGroup(_data, o => o.Status);

    // ── SumByGroup ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public Dictionary<string, decimal> SumByStatus_Linq() =>
        _data.Where(o => o.IsActive)
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Amount));

    [Benchmark]
    public Dictionary<string, decimal> SumByStatus_ValiFlow() =>
        _evalIsActive.EvaluateSumByGroup(_data, o => o.Status, o => o.Amount);

    // ── MinByGroup / MaxByGroup ───────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public Dictionary<string, decimal> MinByStatus_Linq() =>
        _data.Where(o => o.IsActive)
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Min(o => o.Amount));

    [Benchmark]
    public Dictionary<string, decimal> MinByStatus_ValiFlow() =>
        _evalIsActive.EvaluateMinByGroup(_data, o => o.Status, o => o.Amount);

    [Benchmark(Baseline = true)]
    public Dictionary<string, decimal> MaxByStatus_Linq() =>
        _data.Where(o => o.IsActive)
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Max(o => o.Amount));

    [Benchmark]
    public Dictionary<string, decimal> MaxByStatus_ValiFlow() =>
        _evalIsActive.EvaluateMaxByGroup(_data, o => o.Status, o => o.Amount);
}
