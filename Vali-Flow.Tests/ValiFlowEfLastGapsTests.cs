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
/// Last few stragglers: EvaluateQueryFailedAsync's catch block, EvaluateTopAsync combined with
/// pagination, QuerySpecification's 4-arg constructor overload, QuerySpecification.WithTop after
/// Page/PageSize already set, and Validation.ValidateFilterNotNull via a specification with a null Filter
/// (unreachable through BasicSpecification's own constructors, which never allow a null Filter — only a
/// custom ISpecification{T} can produce one).
/// </summary>
public sealed class ValiFlowEfLastGapsTests
{
    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        ctx.Orders.Add(new TestOrder { Id = 1, CustomerName = "Alice", Total = 10m, IsShipped = false, CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        return ctx;
    }

    [Fact]
    public async Task EvaluateQueryFailedAsync_PageWithoutPageSize_RecordsAndRethrows()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id).WithPage(1);

        Func<Task> act = () => ev.EvaluateQueryFailedAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EvaluateTopAsync_CombinedWithPagination_ThrowsInvalidOperationException()
    {
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id).WithPagination(1, 10);

        Func<Task> act = () => ev.EvaluateTopAsync(spec, 5);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void QuerySpecification_FourArgConstructor_SetsOptions()
    {
        var spec = new QuerySpecification<TestOrder>(new ValiFlowQuery<TestOrder>(), asNoTracking: false, asSplitQuery: true);

        spec.AsNoTracking.Should().BeFalse();
        spec.AsSplitQuery.Should().BeTrue();
    }

    [Fact]
    public void WithTop_AfterPageSize_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>().WithPageSize(10);

        Action act = () => spec.WithTop(5);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task EvaluateAnyAsync_NullFilterOnCustomSpecification_RecordsAndRethrows()
    {
        // BasicSpecification's own constructors never allow a null Filter (they either default to a new
        // ValiFlowQuery<T> or throw ArgumentNullException), so QuerySpecificationBuilder's
        // Validation.ValidateFilterNotNull guard is only reachable via a custom ISpecification<T>.
        await using var ctx = await CreateSeededContextAsync();
        var ev = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new RawQuerySpec<TestOrder> { Filter = null! };

        Func<Task> act = () => ev.EvaluateAnyAsync(spec);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
