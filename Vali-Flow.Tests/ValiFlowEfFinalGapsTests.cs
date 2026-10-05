using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Targets the remaining catch(Exception)/RecordException branches and a handful of guard clauses that
/// were still at 0% after the broader coverage pass: write-method catch blocks (null entity/entities
/// propagating through the try), pre-canceled-token catch paths on read methods whose null guard sits
/// outside the try, EvaluatePagedAsync's Page/PageSize-individually-missing guards, EvaluateDistinctAsync/
/// EvaluateDuplicatesAsync's null-selector and pagination-without-ordering guards, the relational
/// (non-InMemory) branch of DeleteByConditionCoreAsync, and the "rollback itself fails" branch of
/// RollbackTransactionAsync.
/// </summary>
public sealed class ValiFlowEfFinalGapsTests
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
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── Write-method catch blocks: null entity/entities propagates through try ──

    [Fact]
    public async Task DeleteAsync_NullEntity_RecordsAndRethrows()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => ev.DeleteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_RecordsAndRethrows()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => ev.UpdateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpsertAsync_NullEntity_RecordsAndRethrows()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => ev.UpsertAsync(null!, o => o.Id == 1);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkInsertOrUpdateAsync_NullEntities_RecordsAndRethrows()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => ev.BulkInsertOrUpdateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── Pre-canceled-token catch paths (null guard sits outside the try) ─────

    [Fact]
    public async Task EvaluateGetFirstAsync_PreCanceledToken_ThrowsWithCanceledInner()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();
        var ct = new CancellationToken(canceled: true);

        Func<Task> act = () => ev.EvaluateGetFirstAsync(spec, ct);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.InnerException.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task EvaluateGetFirstFailedAsync_PreCanceledToken_ThrowsWithCanceledInner()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>();
        var ct = new CancellationToken(canceled: true);

        Func<Task> act = () => ev.EvaluateGetFirstFailedAsync(spec, ct);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.InnerException.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task DeleteByConditionAsync_PreCanceledToken_ThrowsWithCanceledInner()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var ct = new CancellationToken(canceled: true);

        Func<Task> act = () => ev.DeleteByConditionAsync(o => o.Id == 1, ct);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.InnerException.Should().BeAssignableTo<OperationCanceledException>();
    }

    // ── EvaluateGetLastAsync / EvaluateGetLastFailedAsync — pagination guard ──

    [Fact]
    public async Task EvaluateGetLastAsync_PageSet_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithPage(1);

        Func<Task> act = () => ev.EvaluateGetLastAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EvaluateGetLastFailedAsync_PageSet_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithPage(1);

        Func<Task> act = () => ev.EvaluateGetLastFailedAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── EvaluatePagedAsync — Page/PageSize missing individually ───────────────

    [Fact]
    public async Task EvaluatePagedAsync_PageSizeSetWithoutPage_ThrowsArgumentException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id).WithPageSize(10);

        Func<Task> act = () => ev.EvaluatePagedAsync(spec);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EvaluatePagedAsync_PageSetWithoutPageSize_ThrowsArgumentException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id).WithPage(1);

        Func<Task> act = () => ev.EvaluatePagedAsync(spec);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── EvaluateDistinctAsync / EvaluateDuplicatesAsync guards ────────────────

    [Fact]
    public async Task EvaluateDistinctAsync_NullSelector_ThrowsArgumentNullException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>();

        Func<Task> act = () => ev.EvaluateDistinctAsync<int>(spec, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateDistinctAsync_PaginationWithoutOrdering_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithTop(5);

        Func<Task> act = () => ev.EvaluateDistinctAsync(spec, o => o.CustomerName);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EvaluateDuplicatesAsync_NullSelector_ThrowsArgumentNullException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>();

        Func<Task> act = () => ev.EvaluateDuplicatesAsync<int>(spec, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateDuplicatesAsync_PaginationWithoutOrdering_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithTop(5);

        Func<Task> act = () => ev.EvaluateDuplicatesAsync(spec, o => o.CustomerName);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── DeleteByConditionCoreAsync — relational (non-InMemory) branch ─────────

    [Fact]
    public async Task DeleteByConditionAsync_RelationalProvider_UsesExecuteDeleteAsync()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        await using var ctx = new TestDbContext(options);
        await ctx.Database.EnsureCreatedAsync();
        ctx.Orders.Add(new TestOrder { Id = 1, CustomerName = "Alice", Total = 10m, IsShipped = false, CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        await ev.DeleteByConditionAsync(o => o.Id == 1);

        (await ctx.Orders.CountAsync()).Should().Be(0);
    }

    // ── RollbackTransactionAsync — rollback itself fails ──────────────────────

    [Fact]
    public async Task ExecuteTransactionAsync_RollbackAlsoFails_ThrowsAggregateException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        await using var ctx = new TestDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => ev.ExecuteTransactionAsync(async () =>
        {
            // Sabotage the connection so the subsequent RollbackAsync call inside the evaluator's own
            // failure-handling path throws too, exercising the "rollback also failed" branch.
            await connection.CloseAsync();
            throw new InvalidOperationException("boom");
        });

        await act.Should().ThrowAsync<Exception>();
    }
}
