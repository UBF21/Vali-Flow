using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class ValiFlowEfEdgePathTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TestDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ══════════════════════════════════════════════════════════════════════════
    // Section 1 — ExecuteTransactionAsync rollback path (SQLite)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExecuteTransactionAsync_OperationsThrow_RethrowsAsInvalidOperationException()
    {
        await using var sqlite = SqliteTestContext.Create();
        var ev = new ValiFlowEvaluator<TestOrder>(sqlite.Context);

        Func<Task> act = () => ev.ExecuteTransactionAsync(() => throw new Exception("boom"));

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("ExecuteTransactionAsync");
    }

    [Fact]
    public async Task ExecuteTransactionAsync_OperationsSucceed_NoException()
    {
        await using var sqlite = SqliteTestContext.Create();
        var ev = new ValiFlowEvaluator<TestOrder>(sqlite.Context);

        Func<Task> act = () => ev.ExecuteTransactionAsync(async () =>
        {
            await sqlite.Context.Orders.AddAsync(
                new TestOrder { Id = 1, CustomerName = "Alice", Total = 100m, IsShipped = false, CreatedAt = DateTime.UtcNow });
            await sqlite.Context.SaveChangesAsync();
        });

        await act.Should().NotThrowAsync();
        var count = await sqlite.Context.Orders.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTransactionAsync_NullOperations_ThrowsArgumentNullException()
    {
        await using var sqlite = SqliteTestContext.Create();
        var ev = new ValiFlowEvaluator<TestOrder>(sqlite.Context);

        Func<Task> act = () => ev.ExecuteTransactionAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteTransactionAsync_ExistingTransaction_RunsOperationsWithoutBeginNew()
    {
        await using var sqlite = SqliteTestContext.Create();
        var ev = new ValiFlowEvaluator<TestOrder>(sqlite.Context);

        // Manually begin a transaction — evaluator should detect it and skip BeginTransactionAsync
        await using var outerTx = await sqlite.Context.Database.BeginTransactionAsync();

        var called = false;
        Func<Task> act = () => ev.ExecuteTransactionAsync(() =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await act.Should().NotThrowAsync();
        called.Should().BeTrue();

        // Outer transaction is still alive (evaluator should not have committed or rolled it back)
        sqlite.Context.Database.CurrentTransaction.Should().NotBeNull();
        await outerTx.RollbackAsync();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Section 2 — CancellationToken already canceled
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCountAsync_PreCanceledToken_ThrowsWithCanceledInner()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();
        var ct = new CancellationToken(canceled: true);

        Func<Task> act = () => ev.EvaluateCountAsync(spec, ct);

        // The evaluator wraps all exceptions in InvalidOperationException;
        // the inner exception must be OperationCanceledException.
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.InnerException.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task EvaluateAnyAsync_PreCanceledToken_ThrowsWithCanceledInner()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();
        var ct = new CancellationToken(canceled: true);

        Func<Task> act = () => ev.EvaluateAnyAsync(spec, ct);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.InnerException.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task AddAsync_PreCanceledToken_ThrowsWithCanceledInner()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var order = new TestOrder { Id = 1, CustomerName = "Alice", Total = 50m, IsShipped = false, CreatedAt = DateTime.UtcNow };
        var ct = new CancellationToken(canceled: true);

        Func<Task> act = () => ev.AddAsync(order, cancellationToken: ct);

        // AddAsync saves changes internally; SaveChangesAsync checks the token
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.InnerException.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task SaveChangesAsync_PreCanceledToken_ThrowsWithCanceledInner()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var ct = new CancellationToken(canceled: true);

        // Stage a pending change so the InMemory provider actually checks the token
        ctx.Orders.Add(new TestOrder { Id = 99, CustomerName = "Test", Total = 1m, IsShipped = false, CreatedAt = DateTime.UtcNow });

        Func<Task> act = () => ev.SaveChangesAsync(ct);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.InnerException.Should().BeAssignableTo<OperationCanceledException>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Section 3 — Remaining null guards
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExecuteTransactionAsync_NullOperations_ThrowsArgumentNullException_Guard()
    {
        await using var sqlite = SqliteTestContext.Create();
        var ev = new ValiFlowEvaluator<TestOrder>(sqlite.Context);

        Func<Task> act = () => ev.ExecuteTransactionAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*operations*");
    }

    [Fact]
    public async Task UpsertRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => ev.UpsertRangeAsync<int>(null!, o => o.Id);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*entities*");
    }

    [Fact]
    public async Task UpsertRangeAsync_NullKeySelector_ThrowsArgumentNullException()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var entities = new List<TestOrder>
        {
            new() { Id = 1, CustomerName = "Alice", Total = 100m, IsShipped = false, CreatedAt = DateTime.UtcNow }
        };

        Func<Task> act = () => ev.UpsertRangeAsync<int>(entities, null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*keySelector*");
    }

    [Fact]
    public async Task EvaluateAggregateAsync_NullSelector_ThrowsArgumentNullException()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        Func<Task> act = () => ev.EvaluateAggregateAsync<decimal>(spec, null!, (a, b) => a + b);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*selector*");
    }

    [Fact]
    public async Task EvaluateAggregateAsync_NullAggregator_ThrowsArgumentNullException()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();

        Func<Task> act = () => ev.EvaluateAggregateAsync<decimal>(spec, o => o.Total, null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*aggregator*");
    }

    [Fact]
    public async Task DeleteByConditionAsync_NullCondition_ThrowsArgumentNullException()
    {
        await using var ctx = CreateInMemoryContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => ev.DeleteByConditionAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("*condition*");
    }
}
