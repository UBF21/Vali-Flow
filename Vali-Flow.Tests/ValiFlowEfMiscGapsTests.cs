using System.Numerics;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Repository;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Extensions;
using Vali_Flow.Interfaces.Evaluators.Read;
using Vali_Flow.Interfaces.Evaluators.Write;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Grab-bag covering: GenericRepository.SaveChangesAsync, ServiceCollectionExtensions.AddValiFlowEvaluator,
/// the dead decimal constant in Utils.Constants, the ExecuteNumericAggAsync double/float/long/fallback
/// branches (via EvaluateMinAsync/EvaluateMaxAsync), and the ExecuteTransactionAsync "reused existing
/// transaction" failure path (RunWithinExistingTransactionAsync's catch branch).
/// </summary>
public sealed class ValiFlowEfMiscGapsTests
{
    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    private static BasicSpecification<TestOrder> AllOrders() => new(new ValiFlowQuery<TestOrder>());

    // ── GenericRepository.SaveChangesAsync ────────────────────────────────────

    [Fact]
    public async Task GenericRepository_SaveChangesAsync_PersistsPendingChanges()
    {
        await using var ctx = CreateContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var repo = new GenericRepository<TestOrder, int>(evaluator, o => o.Id);

        ctx.Orders.Add(new TestOrder { Id = 1, CustomerName = "Alice", Total = 1m, IsShipped = false, CreatedAt = DateTime.UtcNow });
        await repo.SaveChangesAsync();

        (await ctx.Orders.CountAsync()).Should().Be(1);
    }

    // ── ServiceCollectionExtensions.AddValiFlowEvaluator ───────────────────────

    [Fact]
    public void AddValiFlowEvaluator_RegistersEvaluatorAndReadWriteInterfaces()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddValiFlowEvaluator<TestOrder, TestDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var evaluator = scope.ServiceProvider.GetRequiredService<ValiFlowEvaluator<TestOrder>>();
        var reader = scope.ServiceProvider.GetRequiredService<IEvaluatorRead<TestOrder>>();
        var writer = scope.ServiceProvider.GetRequiredService<IEvaluatorWrite<TestOrder>>();

        evaluator.Should().NotBeNull();
        reader.Should().BeSameAs(evaluator);
        writer.Should().BeSameAs(evaluator);
    }

    // Note: Utils.Constants.ZeroDecimal is unreferenced anywhere in production code and the class is
    // `internal` (no InternalsVisibleTo to Vali-Flow.Tests), so its line is genuinely untestable dead
    // code from this project without touching a .csproj — documented in the final report instead.

    // ── ExecuteNumericAggAsync — double/float/long branches + exotic fallback ─

    [Fact]
    public async Task EvaluateMinAsync_DoubleSelector_ReturnsMin()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMinAsync(AllOrders(), o => (double)o.Total);

        result.Should().Be(80d);
    }

    [Fact]
    public async Task EvaluateMinAsync_FloatSelector_ReturnsMin()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMinAsync(AllOrders(), o => (float)o.Total);

        result.Should().Be(80f);
    }

    [Fact]
    public async Task EvaluateMinAsync_LongSelector_ReturnsMin()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMinAsync(AllOrders(), o => (long)o.Total);

        result.Should().Be(80L);
    }

    [Fact]
    public async Task EvaluateMaxAsync_DoubleSelector_ReturnsMax()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMaxAsync(AllOrders(), o => (double)o.Total);

        result.Should().Be(320d);
    }

    [Fact]
    public async Task EvaluateMaxAsync_FloatSelector_ReturnsMax()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMaxAsync(AllOrders(), o => (float)o.Total);

        result.Should().Be(320f);
    }

    [Fact]
    public async Task EvaluateMaxAsync_LongSelector_ReturnsMax()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMaxAsync(AllOrders(), o => (long)o.Total);

        result.Should().Be(320L);
    }

    [Fact]
    public async Task EvaluateMinAsync_ExoticNumericSelector_FallsBackToInMemoryMin()
    {
        // BigInteger matches none of the five fast-path branches in ExecuteNumericAggAsync,
        // exercising the materialize-and-compute-in-memory fallback.
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMinAsync(AllOrders(), o => (BigInteger)o.Total);

        result.Should().Be(new BigInteger(80));
    }

    [Fact]
    public async Task EvaluateMaxAsync_ExoticNumericSelector_FallsBackToInMemoryMax()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMaxAsync(AllOrders(), o => (BigInteger)o.Total);

        result.Should().Be(new BigInteger(320));
    }

    [Fact]
    public async Task EvaluateMinAsync_ExoticNumericSelector_EmptyResult_ReturnsZero()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await ev.EvaluateMinAsync(AllOrders(), o => (BigInteger)o.Total);

        result.Should().Be(BigInteger.Zero);
    }

    // ── ExecuteTransactionAsync — reused existing transaction, operations throw ──

    [Fact]
    public async Task ExecuteTransactionAsync_ReusedExistingTransaction_OperationThrows_RecordsAndRethrows()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        await using var ctx = new TestDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Begin a transaction manually so ExecuteTransactionAsync detects CurrentTransaction != null and
        // takes the RunWithinExistingTransactionAsync branch instead of starting a new one.
        await using var outerTx = await ctx.Database.BeginTransactionAsync();

        Func<Task> act = () => ev.ExecuteTransactionAsync(() => throw new InvalidOperationException("boom"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        // The evaluator must not have touched the outer transaction — it's still usable.
        await outerTx.RollbackAsync();
    }
}
