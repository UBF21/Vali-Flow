using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>Covers: DeleteByConditionAsync, ExecuteTransactionAsync, EvaluateAsync (entity-level).</summary>
public sealed class ValiFlowEfConditionalTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) },
            new TestOrder { Id = 4, CustomerName = "Dave",  Total = 500m, IsShipped = false, CreatedAt = new DateTime(2024, 4, 1) },
            new TestOrder { Id = 5, CustomerName = "Eve",   Total = 210m, IsShipped = true,  CreatedAt = new DateTime(2024, 5, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── DeleteByConditionAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteByConditionAsync_UnshippedOrders_RemovesTwoRecords()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        await ev.DeleteByConditionAsync(o => !o.IsShipped);

        var remaining = await ctx.Orders.CountAsync();
        remaining.Should().Be(3); // only shipped ones left
        (await ctx.Orders.AnyAsync(o => !o.IsShipped)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteByConditionAsync_TotalAbove400_RemovesDave()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        await ev.DeleteByConditionAsync(o => o.Total > 400m);

        var remaining = await ctx.Orders.CountAsync();
        remaining.Should().Be(4); // Dave (500) removed
        (await ctx.Orders.AnyAsync(o => o.Total > 400m)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteByConditionAsync_NullCondition_ThrowsArgumentNullException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = async () => await ev.DeleteByConditionAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteByConditionAsync_NoMatchingCondition_LeavesAllRecords()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Condition matches nothing
        await ev.DeleteByConditionAsync(o => o.Total > 9999m);

        var count = await ctx.Orders.CountAsync();
        count.Should().Be(5);
    }

    // ── ExecuteTransactionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteTransactionAsync_SuccessfulOperations_PersistsChanges()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        await ev.ExecuteTransactionAsync(async () =>
        {
            var order = await ctx.Orders.FindAsync(1);
            order!.CustomerName = "Alice Tx";
            await ev.UpdateAsync(order, saveChanges: true);

            var newOrder = new TestOrder
            {
                Id = 99, CustomerName = "TxOrder", Total = 1m, IsShipped = false, CreatedAt = DateTime.UtcNow
            };
            await ev.AddAsync(newOrder, saveChanges: true);
        });

        ctx.ChangeTracker.Clear();
        (await ctx.Orders.FindAsync(1))!.CustomerName.Should().Be("Alice Tx");
        (await ctx.Orders.FindAsync(99)).Should().NotBeNull();
        (await ctx.Orders.CountAsync()).Should().Be(6);
    }

    [Fact]
    public async Task ExecuteTransactionAsync_OperationThrows_WrapsExceptionInInvalidOperationException()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = async () => await ev.ExecuteTransactionAsync(() =>
            throw new InvalidOperationException("inner error"));

        // The evaluator wraps it in an outer InvalidOperationException
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteTransactionAsync_NullOperations_ThrowsArgumentNullException()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = async () => await ev.ExecuteTransactionAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── EvaluateAsync (entity-level validation) ───────────────────────────────

    [Fact]
    public async Task EvaluateAsync_EntityMatchesFilter_ReturnsTrue()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true, CreatedAt = DateTime.UtcNow };
        var filter = new ValiFlow<TestOrder>().IsTrue(o => o.IsShipped);

        var result = await ev.EvaluateAsync(filter, order);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_EntityDoesNotMatchFilter_ReturnsFalse()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = new TestOrder { Id = 2, CustomerName = "Bob", Total = 320m, IsShipped = false, CreatedAt = DateTime.UtcNow };
        var filter = new ValiFlow<TestOrder>().IsTrue(o => o.IsShipped);

        var result = await ev.EvaluateAsync(filter, order);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_NullFilter_ThrowsArgumentNullException()
    {
        await using var ctx = CreateContext();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var order = new TestOrder { Id = 1 };

        Func<Task> act = async () => await ev.EvaluateAsync(null!, order);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
