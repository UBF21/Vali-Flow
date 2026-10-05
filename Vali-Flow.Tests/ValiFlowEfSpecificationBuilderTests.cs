using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Core.Utils;
using Vali_Flow.Interfaces.Options;
using Vali_Flow.Interfaces.Specification;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Minimal hand-rolled <see cref="IQuerySpecification{T}"/> used to reach QuerySpecificationBuilder guard
/// branches that are unreachable through QuerySpecification's own fluent API (which keeps the conflicting
/// states mutually exclusive by construction) — e.g. both tracking flags true at once, or Top combined
/// with Page/PageSize on the same specification.
/// </summary>
internal sealed class RawQuerySpec<T> : IQuerySpecification<T> where T : class
{
    public ValiFlowQuery<T> Filter { get; set; } = new();
    public IEnumerable<IEfInclude<T>>? Includes { get; set; }
    public bool AsNoTracking { get; set; } = true;
    public bool AsSplitQuery { get; set; }
    public bool IgnoreQueryFilters { get; set; }
    public bool IgnoreAutoIncludes { get; set; }
    public bool AsNoTrackingWithIdentityResolution { get; set; }
    public string? TagWith { get; set; }
    public IEfOrderBy<T>? OrderBy { get; set; }
    public IEnumerable<IEfOrderThenBy<T>>? ThenBys { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public int? Top { get; set; }
    public ValiSort<T>? ValiSort { get; set; }
}

/// <summary>
/// Covers the fluent setters on BasicSpecification/QuerySpecification directly (most are never exercised
/// by evaluator-level tests because the defaults already satisfy most query paths), plus the
/// EfThenInclude/EfThenIncludeReference ApplyInclude methods and the QuerySpecificationBuilder guard
/// clauses (Top+Paging exclusivity, Page/PageSize must be set together, ThenBy without OrderBy, ValiSort
/// branch, and the actual Skip/Take application).
/// </summary>
public sealed class ValiFlowEfSpecificationBuilderTests
{
    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededContextAsync()
    {
        var ctx = CreateContext();
        var customer = new TestCustomer { Id = 1, Name = "Acme" };
        ctx.Customers.Add(customer);
        ctx.Orders.AddRange(
            new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1), CustomerId = 1 },
            new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1), CustomerId = 1 },
            new TestOrder { Id = 3, CustomerName = "Carol", Total = 80m,  IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1), CustomerId = 1 }
        );
        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── BasicSpecification fluent setters ─────────────────────────────────────

    [Fact]
    public void WithFilter_ReplacesFilter()
    {
        var spec = new BasicSpecification<TestOrder>();
        var newFilter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);

        spec.WithFilter(newFilter);

        spec.Filter.Should().BeSameAs(newFilter);
    }

    [Fact]
    public void WithFilter_Null_ThrowsArgumentNullException()
    {
        var spec = new BasicSpecification<TestOrder>();

        Action act = () => spec.WithFilter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithAsSplitQuery_SetsFlag()
    {
        var spec = new BasicSpecification<TestOrder>().WithAsSplitQuery(true);

        spec.AsSplitQuery.Should().BeTrue();
    }

    [Fact]
    public void WithIgnoreQueryFilters_SetsFlag()
    {
        var spec = new BasicSpecification<TestOrder>().WithIgnoreQueryFilters(true);

        spec.IgnoreQueryFilters.Should().BeTrue();
    }

    [Fact]
    public void WithIgnoreAutoIncludes_SetsFlag()
    {
        var spec = new BasicSpecification<TestOrder>().WithIgnoreAutoIncludes(true);

        spec.IgnoreAutoIncludes.Should().BeTrue();
    }

    [Fact]
    public void WithAsNoTrackingWithIdentityResolution_Enabled_DisablesAsNoTracking()
    {
        var spec = new BasicSpecification<TestOrder>().WithAsNoTrackingWithIdentityResolution(true);

        spec.AsNoTrackingWithIdentityResolution.Should().BeTrue();
        spec.AsNoTracking.Should().BeFalse();
    }

    [Fact]
    public void WithTagWith_SetsTag()
    {
        var spec = new BasicSpecification<TestOrder>().WithTagWith("my-tag");

        spec.TagWith.Should().Be("my-tag");
    }

    [Fact]
    public void AddThenInclude_Null_ThrowsArgumentNullException()
    {
        var spec = new BasicSpecification<TestOrder>();

        Action act = () => spec.AddThenInclude<TestOrderItem, string>(null!, i => i.ProductName);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddThenIncludeReference_Null_ThrowsArgumentNullException()
    {
        var spec = new BasicSpecification<TestOrder>();

        Action act = () => spec.AddThenIncludeReference<TestCustomer, string>(null!, c => c.Name);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddThenInclude_AppliedToQuery_LoadsNestedCollectionNavigation()
    {
        // Root = TestCustomer: Include(Orders) [collection] .ThenInclude(Items) [collection] — two distinct
        // navigations with no cycle back to the root type, unlike Order->Customer->Orders.
        await using var ctx = CreateContext();
        ctx.Customers.Add(new TestCustomer
        {
            Id = 1, Name = "Acme",
            Orders =
            {
                new TestOrder
                {
                    Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true, CreatedAt = DateTime.UtcNow,
                    Items = { new TestOrderItem { Id = 1, OrderId = 1, ProductName = "Widget", UnitPrice = 9.99m } }
                }
            }
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var evaluator = new ValiFlowEvaluator<TestCustomer>(ctx);
        var spec = new QuerySpecification<TestCustomer>(new ValiFlowQuery<TestCustomer>().EqualTo(c => c.Id, 1))
            .AddThenInclude<TestOrder, ICollection<TestOrderItem>>(c => c.Orders, o => o.Items);

        var query = await evaluator.EvaluateQueryAsync(spec);
        var result = query.ToList();

        result.Should().ContainSingle();
        result[0].Orders.Should().ContainSingle();
        result[0].Orders.First().Items.Should().ContainSingle(i => i.ProductName == "Widget");
    }

    [Fact]
    public async Task AddThenIncludeReference_AppliedToQuery_LoadsNestedReferenceNavigation()
    {
        // Root = TestOrderItem: Include(Order) [reference] .ThenInclude(Customer) [reference] — two distinct
        // reference navigations with no cycle.
        await using var ctx = await CreateSeededContextAsync();
        var order = await ctx.Orders.FirstAsync(o => o.Id == 1);
        ctx.OrderItems.Add(new TestOrderItem { Id = 1, OrderId = order.Id, ProductName = "Widget", UnitPrice = 9.99m, Order = order });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var evaluator = new ValiFlowEvaluator<TestOrderItem>(ctx);
        var spec = new QuerySpecification<TestOrderItem>(new ValiFlowQuery<TestOrderItem>().EqualTo(i => i.Id, 1))
            .AddThenIncludeReference<TestOrder, TestCustomer>(i => i.Order, o => o.Customer);

        var query = await evaluator.EvaluateQueryAsync(spec);
        var result = query.ToList();

        result.Should().ContainSingle();
        result[0].Order.Customer.Should().NotBeNull();
        result[0].Order.Customer!.Name.Should().Be("Acme");
    }

    // ── QuerySpecification guard clauses ──────────────────────────────────────

    [Fact]
    public void WithOrderBy_AfterValiSort_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>().WithValiSort(new ValiSort<TestOrder>());

        Action act = () => spec.WithOrderBy(o => o.Id);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddThenBy_WithoutOrderBy_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>();

        Action act = () => spec.AddThenBy(o => o.Id);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddThenBy_AfterValiSort_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id);
        // Force ValiSort onto a fresh spec, then try AddThenBy on one already carrying OrderBy + a direct
        // ValiSort would be contradictory to construct normally, so validate via AddThenBys with ValiSort set.
        var sortedSpec = new QuerySpecification<TestOrder>().WithValiSort(new ValiSort<TestOrder>());

        Action act = () => sortedSpec.AddThenBy(o => o.Id);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddThenBys_WithoutOrderBy_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>();

        Action act = () => spec.AddThenBys(new System.Linq.Expressions.Expression<Func<TestOrder, int>>[] { o => o.Id });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddThenBys_AfterValiSort_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>().WithValiSort(new ValiSort<TestOrder>());

        Action act = () => spec.AddThenBys(new System.Linq.Expressions.Expression<Func<TestOrder, int>>[] { o => o.Id });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddThenBys_WithOrderBySet_AddsAllExpressions()
    {
        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .AddThenBys(new System.Linq.Expressions.Expression<Func<TestOrder, string>>[] { o => o.CustomerName });

        spec.ThenBys.Should().ContainSingle();
    }

    [Fact]
    public void WithPage_AfterTop_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>().WithTop(5);

        Action act = () => spec.WithPage(1);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithPageSize_AfterTop_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>().WithTop(5);

        Action act = () => spec.WithPageSize(10);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithValiSort_AfterOrderBy_ThrowsInvalidOperationException()
    {
        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id);

        Action act = () => spec.WithValiSort(new ValiSort<TestOrder>());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithValiSort_Null_ThrowsArgumentNullException()
    {
        var spec = new QuerySpecification<TestOrder>();

        Action act = () => spec.WithValiSort(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── QuerySpecificationBuilder guards (exercised through the evaluator) ────

    [Fact]
    public async Task EvaluateQueryAsync_AsNoTrackingAndIdentityResolutionBothTrue_ThrowsInvalidOperationException()
    {
        // QuerySpecification's own fluent setters keep AsNoTracking and AsNoTrackingWithIdentityResolution
        // mutually exclusive by construction, so this guard in QuerySpecificationBuilder is only reachable
        // through a custom ISpecification<T> implementation that sets both independently.
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new RawQuerySpec<TestOrder>
        {
            AsNoTracking = true,
            AsNoTrackingWithIdentityResolution = true,
            OrderBy = new global::Vali_Flow.Classes.Options.EfOrderBy<TestOrder, int>(o => o.Id, true)
        };

        Func<Task> act = () => evaluator.EvaluateQueryAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EvaluateQueryAsync_TopCombinedWithPaging_ThrowsInvalidOperationException()
    {
        // WithTop and WithPagination are mutually exclusive via QuerySpecification's own guards, so the
        // QuerySpecificationBuilder.EnsureTopNotCombinedWithPaging guard is only reachable via a custom spec.
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new RawQuerySpec<TestOrder> { Top = 5, Page = 1, PageSize = 10 };

        Func<Task> act = () => evaluator.EvaluateQueryAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EvaluateQueryAsync_PageSetWithoutPageSize_ThrowsInvalidOperationException()
    {
        // WithPage and WithPageSize can be called independently on QuerySpecification, unlike WithPagination
        // — this reaches QuerySpecificationBuilder.EnsurePageAndPageSizeSetTogether through the public API.
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>().WithOrderBy(o => o.Id).WithPage(1);

        Func<Task> act = () => evaluator.EvaluateQueryAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EvaluateQueryAsync_ThenBysWithoutOrderBy_ThrowsInvalidOperationException()
    {
        // QuerySpecification.AddThenBy already requires OrderBy to be set first, so this ApplyOrdering
        // guard in QuerySpecificationBuilder is only reachable via a custom spec with ThenBys populated
        // but OrderBy left null.
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new RawQuerySpec<TestOrder>
        {
            ThenBys = new List<IEfOrderThenBy<TestOrder>>
            {
                new global::Vali_Flow.Classes.Options.EfOrderThenBy<TestOrder, int>(o => o.Id, true)
            }
        };

        Func<Task> act = () => evaluator.EvaluateQueryAsync(spec);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EvaluateQueryAsync_ValiSortSet_AppliesValiSortOrdering()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var sort = new ValiSort<TestOrder>();
        sort.By(o => o.Total, descending: true);
        var spec = new QuerySpecification<TestOrder>().WithValiSort(sort);

        var query = await evaluator.EvaluateQueryAsync(spec);
        var results = query.ToList();

        results.Select(o => o.Total).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task EvaluateQueryAsync_PageAndPageSizeWithOrdering_AppliesSkipTake()
    {
        await using var ctx = await CreateSeededContextAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>()
            .WithOrderBy(o => o.Id)
            .WithPagination(2, 1);

        var query = await evaluator.EvaluateQueryAsync(spec);
        var results = query.ToList();

        results.Should().ContainSingle();
        results[0].Id.Should().Be(2);
    }
}
