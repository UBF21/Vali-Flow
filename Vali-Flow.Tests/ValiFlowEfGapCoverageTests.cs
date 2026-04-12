using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Covers edge cases identified as gaps:
/// - Pagination without OrderBy → InvalidOperationException
/// - Complex AND/OR filter negation
/// - Top(0) returns empty
/// - Top(-1) propagates EF behaviour
/// - Grouped operations with null key values (CustomerId nullable)
/// - DeleteByConditionAsync with always-false condition → 0 deletions
/// - EvaluatePagedAsync on a non-existent page → empty
/// - EvaluateDistinctAsync / EvaluateDuplicatesAsync with nullable selector
/// - ExecuteUpdateAsync with navigation property throws
/// </summary>
public sealed class ValiFlowEfGapCoverageTests
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
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1), CustomerId = 1 },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1), CustomerId = null },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1), CustomerId = 1 },
            new TestOrder { Id = 4, CustomerName = "Dave",  Total = 500m, IsShipped = false, CreatedAt = new DateTime(2024, 4, 1), CustomerId = null },
            new TestOrder { Id = 5, CustomerName = "Eve",   Total = 210m, IsShipped = true,  CreatedAt = new DateTime(2024, 5, 1), CustomerId = 2 }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. Pagination without OrderBy → InvalidOperationException (CRITICAL)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluatePagedAsync_PaginationWithoutOrderBy_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>().WithPagination(1, 3);
        // No WithOrderBy → BuildQuery must reject this

        Func<Task> act = () => ev.EvaluatePagedAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires an ordering*");
    }

    [Fact]
    public async Task EvaluateQueryAsync_PaginationWithoutOrderBy_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>().WithPagination(2, 3);

        Func<Task> act = () => ev.EvaluateQueryAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pagination requires an ordering*");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. Complex AND/OR filter negation (CRITICAL)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateQueryFailedAsync_ComplexAndOrFilter_ReturnsCorrectNegation()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Filter: Total > 200 AND IsShipped
        // Matches: Alice(150,true)=false, Bob(320,false)=false, Carol(80,true)=false, Dave(500,false)=false, Eve(210,true)=true
        // Only Eve matches → "failed" = 4 orders
        var filter = new ValiFlowQuery<TestOrder>()
            .IsTrue(o => o.IsShipped)
            .GreaterThan(o => o.Total, 200m);

        var spec = new QuerySpecification<TestOrder>(filter);

        var query = await ev.EvaluateQueryFailedAsync(spec);
        var results = await query.ToListAsync();

        results.Should().HaveCount(4);
        results.Should().NotContain(o => o.CustomerName == "Eve");
    }

    [Fact]
    public async Task EvaluateQueryFailedAsync_NegatedComplexFilter_CountsCorrectly()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Filter: IsShipped AND Total < 200 → matches Alice(150) and Carol(80) → 2
        // Failed (negated): 3 records
        var filter = new ValiFlowQuery<TestOrder>()
            .IsTrue(o => o.IsShipped)
            .LessThan(o => o.Total, 200m);

        var spec = new QuerySpecification<TestOrder>(filter);

        var query = await ev.EvaluateQueryFailedAsync(spec);
        int negatedCount = await query.CountAsync();

        negatedCount.Should().Be(3); // Bob, Dave, Eve
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. Top(0) and Top with boundary values (HIGH)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateTopAsync_TopZero_ThrowsArgumentOutOfRangeException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>();

        Func<Task> act = () => ev.EvaluateTopAsync(spec, 0);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task EvaluateTopAsync_TopLargerThanDataset_ReturnsAllRecords()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>();

        var query = await ev.EvaluateTopAsync(spec, 1000);
        var results = await query.ToListAsync();

        results.Should().HaveCount(5);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. Grouped operations with null key values (HIGH)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateCountByGroupAsync_GroupsByNonNullableKey_ReturnsCorrectCounts()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new BasicSpecification<TestOrder>();

        // IsShipped: true (Alice, Carol, Eve) = 3, false (Bob, Dave) = 2
        var groups = await ev.EvaluateCountByGroupAsync(spec, o => o.IsShipped);

        groups.Should().HaveCount(2);
        groups[true].Should().Be(3);
        groups[false].Should().Be(2);
    }

    [Fact]
    public async Task EvaluateSumByGroupAsync_GroupsByNonNullableKey_SumsCorrectly()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new BasicSpecification<TestOrder>();

        // IsShipped=true: Alice(150) + Carol(80) + Eve(210) = 440
        // IsShipped=false: Bob(320) + Dave(500) = 820
        var sums = await ev.EvaluateSumByGroupAsync<bool>(spec, o => o.IsShipped, o => o.Total);

        sums[true].Should().Be(440m);
        sums[false].Should().Be(820m);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. DeleteByConditionAsync with always-false condition (MEDIUM)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteByConditionAsync_AlwaysFalseCondition_DeletesNothing()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Condition never true: negative ID
        await ev.DeleteByConditionAsync(o => o.Id < 0);

        var remaining = await ctx.Orders.CountAsync();
        remaining.Should().Be(5);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. EvaluatePagedAsync on a non-existent page (MEDIUM)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluatePagedAsync_PageBeyondData_ReturnsEmptyCollection()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Page 100 of a 5-record dataset → empty
        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithPagination(100, 10);

        var result = await ev.EvaluatePagedAsync(spec);

        result.Items.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. EvaluateDistinctAsync / EvaluateDuplicatesAsync with nullable selector (MEDIUM)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateDistinctAsync_NullableSelector_IncludesNullGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>();

        // CustomerId values: 1,null,1,null,2 → distinct = {1, null, 2} = 3 groups
        var distinct = await ev.EvaluateDistinctAsync(spec, o => o.CustomerId);

        distinct.Should().HaveCount(3);
    }

    [Fact]
    public async Task EvaluateDuplicatesAsync_NullableSelector_ReturnsGroupsWithMoreThanOneRecord()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        var spec = new QuerySpecification<TestOrder>();

        // CustomerId=1: 2 records (Alice, Carol) → duplicate group → 2 entities returned
        // CustomerId=null: 2 records (Bob, Dave) → duplicate group → 2 entities returned
        // CustomerId=2: 1 record (Eve) → NOT duplicate → excluded
        var duplicates = await ev.EvaluateDuplicatesAsync(spec, o => o.CustomerId);

        // Returns all entities from duplicate groups: 2 (group 1) + 2 (group null) = 4
        duplicates.Should().HaveCount(4);
        duplicates.Select(o => o.CustomerId).Should().NotContain(2);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. ExecuteUpdateAsync with navigation property throws (CRITICAL)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ExecuteUpdateAsync_NavigationPropertyInSetProperty_ThrowsException()
    {
        using var sqlite = SqliteTestContext.Create();
        var ctx = sqlite.Context;

        ctx.Orders.Add(new TestOrder
        {
            Id = 1, CustomerName = "Alice", Total = 100m,
            IsShipped = false, CreatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // EF Core does not support SetProperty on navigation properties
        Func<Task> act = () => ev.ExecuteUpdateAsync(
            o => o.Id == 1,
            s => s.SetProperty(o => o.Customer!.Name, "x"));

        // Must throw — navigation properties are not supported by ExecuteUpdateAsync
        await act.Should().ThrowAsync<Exception>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. Top + ThenBy combination (additional ordering coverage)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EvaluateTopByGroupAsync_TopOne_ReturnsHighestPerGroup()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);

        // Order by Total descending so the first item per group is the highest
        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Total, ascending: false)
            .WithTop(1);

        var topPerGroup = await ev.EvaluateTopByGroupAsync<bool>(spec, o => o.IsShipped);

        topPerGroup.Should().HaveCount(2);
        // true group: highest Total = 210 (Eve)
        topPerGroup[true].Should().HaveCount(1);
        topPerGroup[true].First().Total.Should().Be(210m);
        // false group: highest Total = 500 (Dave)
        topPerGroup[false].Should().HaveCount(1);
        topPerGroup[false].First().Total.Should().Be(500m);
    }

    [Fact]
    public void TopAndPaginationMutuallyExclusive_ThrowsOnSpecConstruction()
    {
        // QuerySpecification rejects Top + Pagination at build time
        Action act = () => new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithTop(2)
            .WithPagination(1, 3);

        act.Should().Throw<InvalidOperationException>();
    }
}
