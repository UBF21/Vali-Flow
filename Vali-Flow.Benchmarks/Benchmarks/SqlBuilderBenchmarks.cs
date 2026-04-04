using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Vali_Flow.Benchmarks.Models;
using Vali_Flow.Core.Builder;
using Vali_Flow.Sql.Builder;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;

namespace Vali_Flow.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class SqlBuilderBenchmarks
{
    private ISqlDialect _sqlServer = null!;
    private ISqlDialect _postgres = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sqlServer = new SqlServerDialect();
        _postgres = new PostgreSqlDialect();
    }

    [Benchmark(OperationsPerInvoke = 1000)]
    public SqlQueryResult SelectWithJoins_SqlServer() =>
        new SqlQueryBuilder<BenchmarkOrder>(_sqlServer)
            .From("Orders", "o")
            .InnerJoin("Customers", "c", "o.Id = c.Id")
            .Build();

    [Benchmark(OperationsPerInvoke = 1000)]
    public SqlQueryResult SelectWithPagination_Postgres() =>
        new SqlQueryBuilder<BenchmarkOrder>(_postgres)
            .From("Orders", "o")
            .OrderBy(o => o.Amount, ascending: false)
            .Page(1, 50)
            .Build();

    [Benchmark(OperationsPerInvoke = 1000)]
    public SqlQueryResult InsertBuild() =>
        new SqlInsertBuilder<BenchmarkOrder>(_sqlServer)
            .Into("Orders")
            .Set(o => o.Amount, 99.99m)
            .Set(o => o.IsActive, true)
            .Build();

    [Benchmark(OperationsPerInvoke = 1000)]
    public SqlQueryResult UpdateBuild() =>
        new SqlUpdateBuilder<BenchmarkOrder>(_sqlServer)
            .Table("Orders")
            .Set(o => o.Amount, 150m)
            .AllowUpdateAll()
            .Build();
}
