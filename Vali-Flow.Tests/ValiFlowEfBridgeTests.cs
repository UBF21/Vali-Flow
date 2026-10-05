using System.Numerics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Abstractions.Interfaces;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Covers ValiFlowEvaluator.Bridge.cs — the explicit IQueryReader&lt;T&gt;/IQueryAggregator&lt;T&gt;
/// implementations reachable only by casting the evaluator to those provider-agnostic interfaces
/// (the public spec-based overloads with the same names are separate members, covered elsewhere).
/// </summary>
public sealed class ValiFlowEfBridgeTests
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

    // ── IQueryReader<T>.EvaluateAnyAsync / EvaluateCountAsync / EvaluateGetFirstAsync ──

    [Fact]
    public async Task IQueryReader_EvaluateAnyAsync_WithFilter_ReturnsTrue()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryReader<TestOrder> reader = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await reader.EvaluateAnyAsync(new ValiFlow<TestOrder>().IsTrue(o => o.IsShipped));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IQueryReader_EvaluateAnyAsync_NoFilter_ReturnsTrue()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryReader<TestOrder> reader = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await reader.EvaluateAnyAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IQueryReader_EvaluateCountAsync_WithFilter_ReturnsMatchingCount()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryReader<TestOrder> reader = new ValiFlowEvaluator<TestOrder>(ctx);

        var count = await reader.EvaluateCountAsync(new ValiFlow<TestOrder>().IsTrue(o => o.IsShipped));

        count.Should().Be(2);
    }

    [Fact]
    public async Task IQueryReader_EvaluateGetFirstAsync_NoFilter_ReturnsAnEntity()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryReader<TestOrder> reader = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await reader.EvaluateGetFirstAsync();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task IQueryReader_EvaluateGetLastAsync_NoFilter_ReturnsLastByPrimaryKey()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryReader<TestOrder> reader = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await reader.EvaluateGetLastAsync();

        result.Should().NotBeNull();
        result!.Id.Should().Be(3);
    }

    [Fact]
    public async Task IQueryReader_EvaluateGetLastAsync_WithFilter_ReturnsLastMatching()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryReader<TestOrder> reader = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await reader.EvaluateGetLastAsync(new ValiFlow<TestOrder>().IsTrue(o => o.IsShipped));

        result.Should().NotBeNull();
        result!.Id.Should().Be(3);
    }

    // ── IQueryAggregator<T>.EvaluateMinAsync / EvaluateMaxAsync ───────────────────

    [Fact]
    public async Task IQueryAggregator_EvaluateMinAsync_EmptyResult_ReturnsDefault()
    {
        await using var ctx = CreateContext();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateMinAsync(o => o.Total);

        result.Should().Be(0m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateMinAsync_WithData_ReturnsMin()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateMinAsync(o => o.Total);

        result.Should().Be(80m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateMaxAsync_EmptyResult_ReturnsDefault()
    {
        await using var ctx = CreateContext();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateMaxAsync(o => o.Total);

        result.Should().Be(0m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateMaxAsync_WithFilter_ReturnsMax()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateMaxAsync(o => o.Total, new ValiFlow<TestOrder>().IsTrue(o => o.IsShipped));

        result.Should().Be(150m);
    }

    // ── IQueryAggregator<T>.EvaluateAverageAsync — one branch per supported numeric type ──

    [Fact]
    public async Task IQueryAggregator_EvaluateAverageAsync_EmptyResult_ReturnsZero()
    {
        await using var ctx = CreateContext();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateAverageAsync(o => o.Total);

        result.Should().Be(0m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateAverageAsync_DecimalSelector_ReturnsAverage()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateAverageAsync(o => o.Total);

        result.Should().BeApproximately(183.33m, 0.01m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateAverageAsync_DoubleSelector_ReturnsAverage()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateAverageAsync(o => (double)o.Total);

        result.Should().BeApproximately(183.33m, 0.01m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateAverageAsync_FloatSelector_ReturnsAverage()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateAverageAsync(o => (float)o.Total);

        result.Should().BeApproximately(183.33m, 0.1m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateAverageAsync_LongSelector_ReturnsAverage()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateAverageAsync(o => (long)o.Total);

        result.Should().BeApproximately(183.33m, 0.01m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateAverageAsync_IntSelector_ReturnsAverage()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateAverageAsync(o => (int)o.Total);

        result.Should().BeApproximately(183.33m, 0.01m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateAverageAsync_ExoticNumericSelector_FallsBackToInMemory()
    {
        // BigInteger implements INumber<BigInteger> but matches none of the Average fast-path overloads,
        // exercising the materialize-and-average fallback branch.
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateAverageAsync(o => (BigInteger)o.Total);

        result.Should().BeApproximately(183.33m, 0.01m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateAverageAsync_ExoticNumericSelector_EmptyResult_ReturnsZero()
    {
        await using var ctx = CreateContext();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateAverageAsync(o => (BigInteger)o.Total);

        result.Should().Be(0m);
    }

    // ── IQueryAggregator<T>.EvaluateSumAsync — one branch per supported numeric type + fallback ──

    [Fact]
    public async Task IQueryAggregator_EvaluateSumAsync_DecimalSelector_ReturnsSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateSumAsync(o => o.Total);

        result.Should().Be(550m);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateSumAsync_DoubleSelector_ReturnsSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateSumAsync(o => (double)o.Total);

        result.Should().Be(550d);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateSumAsync_FloatSelector_ReturnsSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateSumAsync(o => (float)o.Total);

        result.Should().Be(550f);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateSumAsync_LongSelector_ReturnsSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateSumAsync(o => (long)o.Total);

        result.Should().Be(550L);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateSumAsync_IntSelector_ReturnsSum()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateSumAsync(o => (int)o.Total);

        result.Should().Be(550);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateSumAsync_ExoticNumericSelector_EmptyResult_ReturnsZero()
    {
        await using var ctx = CreateContext();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateSumAsync(o => (BigInteger)o.Total);

        result.Should().Be(BigInteger.Zero);
    }

    [Fact]
    public async Task IQueryAggregator_EvaluateSumAsync_ExoticNumericSelector_FallsBackToInMemory()
    {
        await using var ctx = await CreateSeededContextAsync();
        IQueryAggregator<TestOrder> aggregator = new ValiFlowEvaluator<TestOrder>(ctx);

        var result = await aggregator.EvaluateSumAsync(o => (BigInteger)o.Total);

        result.Should().Be(new BigInteger(550));
    }
}
