using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

namespace Vali_Flow.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class InMemoryBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int N;

    private List<BenchmarkOrder> _data = null!;
    private ValiFlowEvaluator<BenchmarkOrder, int> _evaluator = null!;
    private readonly Consumer _consumer = new();

    [GlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(1, N)
            .Select(i => new BenchmarkOrder { Id = i, Amount = i * 1.5m, IsActive = i % 2 == 0 })
            .ToList();
        var filter = new ValiFlow<BenchmarkOrder>().IsTrue(o => o.IsActive);
        _evaluator = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filter, o => o.Id);
    }

    [Benchmark]
    public int EvaluateCount() => _evaluator.EvaluateCount(_data);

    [Benchmark]
    public void EvaluateAll() => _evaluator.EvaluateAll<int>(_data).Consume(_consumer);

    [Benchmark]
    public void EvaluateTop() => _evaluator.EvaluateTop<int>(_data, 100).Consume(_consumer);

    [Benchmark]
    public void EvaluatePaged() => _evaluator.EvaluatePaged<int>(_data, 1, 50).Consume(_consumer);

    [Benchmark]
    public decimal EvaluateSum() => _evaluator.EvaluateSum(_data, o => o.Amount);

    [Benchmark]
    public decimal EvaluateAverage() => _evaluator.EvaluateAverage(_data, o => o.Amount);

    [Benchmark]
    public Dictionary<bool, List<BenchmarkOrder>> EvaluateGrouped() =>
        _evaluator.EvaluateGrouped(_data, o => o.IsActive);
}
