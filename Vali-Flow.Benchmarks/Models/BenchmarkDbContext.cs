using Microsoft.EntityFrameworkCore;

namespace Vali_Flow.Benchmarks.Models;

/// <summary>
/// Minimal DbContext backed by the EF Core InMemory provider, used only to measure
/// ValiFlowEvaluator&lt;T&gt; query-building/translation overhead — not real I/O.
/// </summary>
public sealed class BenchmarkDbContext : DbContext
{
    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options)
    {
    }

    public DbSet<BenchmarkOrder> Orders => Set<BenchmarkOrder>();
}
