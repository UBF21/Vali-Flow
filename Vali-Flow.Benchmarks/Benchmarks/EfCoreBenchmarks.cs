using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;

namespace Vali_Flow.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for ValiFlowEvaluator&lt;T&gt; (Vali-Flow, EF Core package) against the EF Core
/// InMemory provider. Measures query-building + LINQ translation overhead vs. hand-written
/// LINQ-to-Entities, not real I/O (no real database is involved).
/// Baseline: filtered query enumeration. Paged/Count are secondary references.
/// </summary>
[MemoryDiagnoser]
public class EfCoreBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int N;

    private BenchmarkDbContext _dbContext = null!;
    private readonly Consumer _consumer = new();

    private ValiFlowEvaluator<BenchmarkOrder> _evaluator = null!;
    private QuerySpecification<BenchmarkOrder> _filterSpec = null!;
    private QuerySpecification<BenchmarkOrder> _pagedSpec = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
            .UseInMemoryDatabase(databaseName: $"vali-flow-bench-{N}")
            .Options;

        _dbContext = new BenchmarkDbContext(options);
        _dbContext.Orders.AddRange(BenchmarkDataFactory.Generate(N));
        _dbContext.SaveChanges();

        _evaluator = new ValiFlowEvaluator<BenchmarkOrder>(_dbContext);

        _filterSpec = new QuerySpecification<BenchmarkOrder>(
            new ValiFlowQuery<BenchmarkOrder>().IsTrue(o => o.IsActive));

        _pagedSpec = new QuerySpecification<BenchmarkOrder>(
                new ValiFlowQuery<BenchmarkOrder>().IsTrue(o => o.IsActive))
            .WithOrderBy(o => o.Id)
            .WithPagination(1, 50);
    }

    [GlobalCleanup]
    public void Cleanup() => _dbContext.Dispose();

    // ── Filter (baseline) ─────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public void FilterAll_IsActive_Linq() =>
        _dbContext.Orders.Where(o => o.IsActive).Consume(_consumer);

    [Benchmark]
    public async Task FilterAll_IsActive_ValiFlow()
    {
        var query = await _evaluator.EvaluateQueryAsync(_filterSpec);
        query.Consume(_consumer);
    }

    // ── Paged (reference — no ratio) ──────────────────────────────────────────

    [Benchmark]
    public void Page1x50_IsActive_Linq() =>
        _dbContext.Orders.Where(o => o.IsActive).OrderBy(o => o.Id).Skip(0).Take(50).Consume(_consumer);

    [Benchmark]
    public async Task<PagedResultLength> Page1x50_IsActive_ValiFlow()
    {
        var result = await _evaluator.EvaluatePagedAsync(_pagedSpec);
        return new PagedResultLength(result.Items.Count);
    }

    // ── Count (reference — no ratio) ──────────────────────────────────────────

    [Benchmark]
    public int Count_IsActive_Linq() => _dbContext.Orders.Count(o => o.IsActive);

    [Benchmark]
    public Task<int> Count_IsActive_ValiFlow() => _evaluator.EvaluateCountAsync(_filterSpec);

    public readonly record struct PagedResultLength(int Count);
}
