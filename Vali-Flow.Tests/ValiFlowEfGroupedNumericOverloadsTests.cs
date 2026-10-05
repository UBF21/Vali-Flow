using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Covers the long/float/double overloads of EvaluateSumByGroupAsync/EvaluateMinByGroupAsync/
/// EvaluateMaxByGroupAsync. The int and decimal overloads are already covered by
/// ValiFlowEfGroupedAsyncTests/ValiFlowEfGroupedTests — every overload shares the same body shape
/// (just a different selector type), so one happy-path test per overload is enough to hit its branch.
/// </summary>
public sealed class ValiFlowEfGroupedNumericOverloadsTests
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

    // ── EvaluateSumByGroupAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateSumByGroupAsync_LongSelector_ByIsShipped_CorrectSums()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var sums = await ev.EvaluateSumByGroupAsync(AllOrders(), o => o.IsShipped, o => (long)o.Total);

        sums[true].Should().Be(230L);
        sums[false].Should().Be(320L);
    }

    [Fact]
    public async Task EvaluateSumByGroupAsync_FloatSelector_ByIsShipped_CorrectSums()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var sums = await ev.EvaluateSumByGroupAsync(AllOrders(), o => o.IsShipped, o => (float)o.Total);

        sums[true].Should().Be(230f);
        sums[false].Should().Be(320f);
    }

    [Fact]
    public async Task EvaluateSumByGroupAsync_DoubleSelector_ByIsShipped_CorrectSums()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var sums = await ev.EvaluateSumByGroupAsync(AllOrders(), o => o.IsShipped, o => (double)o.Total);

        sums[true].Should().Be(230d);
        sums[false].Should().Be(320d);
    }

    // ── EvaluateMinByGroupAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMinByGroupAsync_LongSelector_ByIsShipped_CorrectMins()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var mins = await ev.EvaluateMinByGroupAsync(AllOrders(), o => o.IsShipped, o => (long)o.Total);

        mins[true].Should().Be(80L);
        mins[false].Should().Be(320L);
    }

    [Fact]
    public async Task EvaluateMinByGroupAsync_FloatSelector_ByIsShipped_CorrectMins()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var mins = await ev.EvaluateMinByGroupAsync(AllOrders(), o => o.IsShipped, o => (float)o.Total);

        mins[true].Should().Be(80f);
        mins[false].Should().Be(320f);
    }

    [Fact]
    public async Task EvaluateMinByGroupAsync_DoubleSelector_ByIsShipped_CorrectMins()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var mins = await ev.EvaluateMinByGroupAsync(AllOrders(), o => o.IsShipped, o => (double)o.Total);

        mins[true].Should().Be(80d);
        mins[false].Should().Be(320d);
    }

    // ── EvaluateMaxByGroupAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateMaxByGroupAsync_LongSelector_ByIsShipped_CorrectMaxes()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var maxes = await ev.EvaluateMaxByGroupAsync(AllOrders(), o => o.IsShipped, o => (long)o.Total);

        maxes[true].Should().Be(150L);
        maxes[false].Should().Be(320L);
    }

    [Fact]
    public async Task EvaluateMaxByGroupAsync_FloatSelector_ByIsShipped_CorrectMaxes()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var maxes = await ev.EvaluateMaxByGroupAsync(AllOrders(), o => o.IsShipped, o => (float)o.Total);

        maxes[true].Should().Be(150f);
        maxes[false].Should().Be(320f);
    }

    [Fact]
    public async Task EvaluateMaxByGroupAsync_DoubleSelector_ByIsShipped_CorrectMaxes()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var maxes = await ev.EvaluateMaxByGroupAsync(AllOrders(), o => o.IsShipped, o => (double)o.Total);

        maxes[true].Should().Be(150d);
        maxes[false].Should().Be(320d);
    }
}
