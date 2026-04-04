using BenchmarkDotNet.Attributes;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;

namespace Vali_Flow.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for InMemory write operations: Add, AddRange, Update, Delete, Upsert.
/// Uses [IterationSetup] to restore list state between iterations so measurements are stable.
/// </summary>
[MemoryDiagnoser]
public class InMemoryWriteBenchmarks
{
    private static readonly string[] Statuses = ["Pending", "Completed", "Cancelled"];

    [Params(100, 1_000, 10_000)]
    public int N;

    private List<BenchmarkOrder> _snapshot = null!;
    private List<BenchmarkOrder> _data     = null!;

    private ValiFlowEvaluator<BenchmarkOrder, int> _evaluator = null!;

    private BenchmarkOrder _newOrder       = null!;
    private List<BenchmarkOrder> _newBatch = null!;

    [GlobalSetup]
    public void Setup()
    {
        var baseDate = new DateTime(2024, 1, 1);
        _snapshot = Enumerable.Range(1, N)
            .Select(i => new BenchmarkOrder
            {
                Id        = i,
                Amount    = i * 1.5m,
                IsActive  = i % 2 == 0,
                Name      = $"Order-{i}",
                Status    = Statuses[i % 3],
                Quantity  = i % 200,
                CreatedAt = baseDate.AddDays(i % 365)
            })
            .ToList();

        _newOrder = new BenchmarkOrder { Id = N + 1, Amount = 999m, IsActive = true, Name = "New", Status = "Pending", Quantity = 1 };
        _newBatch = Enumerable.Range(N + 1, 10)
            .Select(i => new BenchmarkOrder { Id = i, Amount = i * 2m, IsActive = true, Name = $"Batch-{i}", Status = "Completed", Quantity = i % 50 })
            .ToList();
    }

    [IterationSetup]
    public void ResetData()
    {
        _data = [.._snapshot];
        var filter = new ValiFlow<BenchmarkOrder>().IsTrue(o => o.IsActive);
        _evaluator = new ValiFlowEvaluator<BenchmarkOrder, int>(_data, filter, o => o.Id);
    }

    // ── Add ──────────────────────────────────────────────────────────────────

    [Benchmark]
    public bool Add_Single() => _evaluator.Add(_newOrder, _data);

    [Benchmark]
    public void AddRange_10() => _evaluator.AddRange(_newBatch, _data);

    // ── Update ───────────────────────────────────────────────────────────────

    [Benchmark]
    public BenchmarkOrder? Update_Single()
    {
        var target = _data[N / 2];
        target.Amount = 1234m;
        return _evaluator.Update(target, _data);
    }

    [Benchmark]
    public void UpdateRange_10()
    {
        var batch = _data.Take(10).ToList();
        foreach (var o in batch) o.Amount += 1m;
        _evaluator.UpdateRange(batch, _data);
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Benchmark]
    public bool Delete_Single() => _evaluator.Delete(_data[0], _data);

    [Benchmark]
    public int DeleteRange_10() => _evaluator.DeleteRange(_data.Take(10).ToList(), _data);

    // ── Upsert ───────────────────────────────────────────────────────────────

    [Benchmark]
    public BenchmarkOrder Upsert_Existing()
    {
        var existing = _data[N / 2];
        existing.Amount = 9999m;
        return _evaluator.Upsert(existing, _data);
    }

    [Benchmark]
    public BenchmarkOrder Upsert_New() => _evaluator.Upsert(_newOrder, _data);

    [Benchmark]
    public void UpsertRange_10()
    {
        var toUpsert = _data.Take(5)
            .Concat(_newBatch.Take(5))
            .ToList();
        _evaluator.UpsertRange(toUpsert, _data);
    }
}
