using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>Covers: EvaluateAsync (entity-level), EvaluateGetFirstFailedAsync, EvaluateGetLastFailedAsync, EvaluateQueryFailedAsync, ExecuteUpdateAsync.</summary>
public sealed class ValiFlowEfFailedAndExecuteTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m,  IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m,  IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,   IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) },
            new TestOrder { Id = 4, CustomerName = "Dave",  Total = 500m,  IsShipped = false, CreatedAt = new DateTime(2024, 4, 1) }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── EvaluateAsync (entity-level) ──────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_EntitySatisfiesFilter_ReturnsTrue()
    {
        await using var ctx = CreateContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlow<TestOrder>().IsTrue(o => o.IsShipped);
        var entity = new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true, CreatedAt = new DateTime(2024, 1, 1) };

        var result = await evaluator.EvaluateAsync(filter, entity);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_EntityDoesNotSatisfyFilter_ReturnsFalse()
    {
        await using var ctx = CreateContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlow<TestOrder>().IsTrue(o => o.IsShipped);
        var entity = new TestOrder { Id = 2, CustomerName = "Bob", Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) };

        var result = await evaluator.EvaluateAsync(filter, entity);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_NullValiFlow_ThrowsArgumentNullException()
    {
        await using var ctx = CreateContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var entity = new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true, CreatedAt = new DateTime(2024, 1, 1) };

        Func<Task> act = async () => await evaluator.EvaluateAsync(null!, entity);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── EvaluateGetFirstFailedAsync ───────────────────────────────────────────

    [Fact]
    public async Task EvaluateGetFirstFailedAsync_WithIsShippedFilter_ReturnsFirstUnshippedOrder()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new BasicSpecification<TestOrder>(filter);

        var result = await evaluator.EvaluateGetFirstFailedAsync(spec);

        result.Should().NotBeNull();
        result!.IsShipped.Should().BeFalse();
        result.CustomerName.Should().Be("Bob");
    }

    [Fact]
    public async Task EvaluateGetFirstFailedAsync_AllSatisfyFilter_ReturnsNull()
    {
        await using var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true, CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true, CreatedAt = new DateTime(2024, 3, 1) }
        );
        await ctx.SaveChangesAsync();

        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new BasicSpecification<TestOrder>(filter);

        var result = await evaluator.EvaluateGetFirstFailedAsync(spec);

        result.Should().BeNull();
    }

    // ── EvaluateGetLastFailedAsync ────────────────────────────────────────────

    [Fact]
    public async Task EvaluateGetLastFailedAsync_WithIsShippedFilter_ReturnsLastUnshippedOrder()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new QuerySpecification<TestOrder>(filter)
            .WithOrderBy<int>(o => o.Id);

        var result = await evaluator.EvaluateGetLastFailedAsync(spec);

        result.Should().NotBeNull();
        result!.IsShipped.Should().BeFalse();
        result.CustomerName.Should().Be("Dave");
    }

    [Fact]
    public async Task EvaluateGetLastFailedAsync_AllSatisfyFilter_ReturnsNull()
    {
        await using var ctx = CreateContext();
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true, CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true, CreatedAt = new DateTime(2024, 3, 1) }
        );
        await ctx.SaveChangesAsync();

        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new QuerySpecification<TestOrder>(filter)
            .WithOrderBy<int>(o => o.Id);

        var result = await evaluator.EvaluateGetLastFailedAsync(spec);

        result.Should().BeNull();
    }

    // ── EvaluateQueryFailedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task EvaluateQueryFailedAsync_WithIsShippedFilter_ReturnsOnlyUnshippedOrders()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new QuerySpecification<TestOrder>(filter);

        var query = await evaluator.EvaluateQueryFailedAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(2);
        results.Should().OnlyContain(o => !o.IsShipped);
        results.Select(o => o.CustomerName).Should().BeEquivalentTo(new[] { "Bob", "Dave" });
    }

    // ── ExecuteUpdateAsync (SQLite) ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteUpdateAsync_ShippedOrders_UpdatesTotalAndReturnsCount()
    {
        using var sqlite = SqliteTestContext.Create();
        var ctx = sqlite.Context;

        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m,  IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m,  IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,   IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1) },
            new TestOrder { Id = 4, CustomerName = "Dave",  Total = 500m,  IsShipped = false, CreatedAt = new DateTime(2024, 4, 1) }
        );
        await ctx.SaveChangesAsync();

        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var updatedCount = await evaluator.ExecuteUpdateAsync(
            o => o.IsShipped,
            s => s.SetProperty(o => o.Total, 999m)
        );

        updatedCount.Should().Be(2);

        var updatedOrders = await ctx.Orders.AsNoTracking().Where(o => o.IsShipped).ToListAsync();
        updatedOrders.Should().OnlyContain(o => o.Total == 999m);

        var untouchedOrders = await ctx.Orders.AsNoTracking().Where(o => !o.IsShipped).ToListAsync();
        untouchedOrders.Should().OnlyContain(o => o.Total != 999m);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_ConditionMatchesNothing_ReturnsZero()
    {
        using var sqlite = SqliteTestContext.Create();
        var ctx = sqlite.Context;

        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1) },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1) }
        );
        await ctx.SaveChangesAsync();

        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var updatedCount = await evaluator.ExecuteUpdateAsync(
            o => o.Total > 10_000m,
            s => s.SetProperty(o => o.Total, 0m)
        );

        updatedCount.Should().Be(0);
    }
}
