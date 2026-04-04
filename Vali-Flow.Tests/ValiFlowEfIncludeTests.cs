using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

public sealed class ValiFlowEfIncludeTests
{
    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<TestDbContext> CreateSeededWithRelationsAsync()
    {
        var ctx = CreateContext();

        var customer1 = new TestCustomer { Id = 1, Name = "Alice" };
        var customer2 = new TestCustomer { Id = 2, Name = "Bob" };
        ctx.Customers.AddRange(customer1, customer2);

        var order1 = new TestOrder { Id = 1, CustomerName = "Alice", Total = 150m, IsShipped = true,  CreatedAt = new DateTime(2024, 1, 1), CustomerId = 1 };
        var order2 = new TestOrder { Id = 2, CustomerName = "Bob",   Total = 320m, IsShipped = false, CreatedAt = new DateTime(2024, 2, 1), CustomerId = 2 };
        var order3 = new TestOrder { Id = 3, CustomerName = "Alice", Total = 80m,  IsShipped = true,  CreatedAt = new DateTime(2024, 3, 1), CustomerId = 1 };
        ctx.Orders.AddRange(order1, order2, order3);

        ctx.OrderItems.AddRange(
            new TestOrderItem { Id = 1, OrderId = 1, ProductName = "Widget",    UnitPrice = 50m  },
            new TestOrderItem { Id = 2, OrderId = 1, ProductName = "Gadget",    UnitPrice = 100m },
            new TestOrderItem { Id = 3, OrderId = 2, ProductName = "Doohickey", UnitPrice = 320m }
        );

        await ctx.SaveChangesAsync();
        return ctx;
    }

    // ── Include tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Include_CustomerNavigation_LoadsRelatedCustomer()
    {
        await using var ctx = await CreateSeededWithRelationsAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().EqualTo(o => o.Id, 1);
        var spec = new BasicSpecification<TestOrder>(filter)
            .AddInclude(o => o.Customer);

        var result = await evaluator.EvaluateGetFirstAsync(spec);

        result.Should().NotBeNull();
        result!.Customer.Should().NotBeNull();
        result.Customer!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task Include_MultipleNavigations_LoadsBothRelated()
    {
        await using var ctx = await CreateSeededWithRelationsAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().EqualTo(o => o.Id, 1);
        var spec = new BasicSpecification<TestOrder>(filter)
            .AddInclude(o => o.Customer)
            .AddInclude(o => o.Items);

        var result = await evaluator.EvaluateGetFirstAsync(spec);

        result.Should().NotBeNull();
        result!.Customer.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ThenInclude_CollectionNav_LoadsNestedItems()
    {
        await using var ctx = await CreateSeededWithRelationsAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().EqualTo(o => o.CustomerName, "Alice");
        var spec = new QuerySpecification<TestOrder>(filter)
            .AddInclude(o => o.Items);

        var results = (await evaluator.EvaluateQueryAsync(spec)).ToList();

        // Alice has 2 orders; order 1 has 2 items
        results.Should().HaveCount(2);
        results.First(o => o.Id == 1).Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddThenInclude_ChainedWithAddInclude_BothLoaded()
    {
        await using var ctx = await CreateSeededWithRelationsAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().EqualTo(o => o.Id, 1);
        var spec = new BasicSpecification<TestOrder>(filter)
            .AddInclude(o => o.Customer)
            .AddInclude(o => o.Items);

        var result = await evaluator.EvaluateGetFirstAsync(spec);

        result.Should().NotBeNull();
        result!.Customer.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Include_WithFilter_OnlyMatchingEntitiesHaveNavLoaded()
    {
        await using var ctx = await CreateSeededWithRelationsAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped);
        var spec = new QuerySpecification<TestOrder>(filter)
            .AddInclude(o => o.Customer);

        var results = (await evaluator.EvaluateQueryAsync(spec)).ToList();

        results.Should().OnlyContain(o => o.IsShipped);
        results.Should().AllSatisfy(o => o.Customer.Should().NotBeNull());
    }

    [Fact]
    public async Task Include_WithAsNoTracking_DoesNotTrackEntities()
    {
        await using var ctx = await CreateSeededWithRelationsAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().EqualTo(o => o.Id, 1);
        var spec = new BasicSpecification<TestOrder>(filter, asNoTracking: true)
            .AddInclude(o => o.Customer);

        var result = await evaluator.EvaluateGetFirstAsync(spec);

        result.Should().NotBeNull();
        // With AsNoTracking, entity state should be Detached
        ctx.Entry(result!).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task Include_AllOrders_LoadsCustomerForEach()
    {
        await using var ctx = await CreateSeededWithRelationsAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>();
        var spec = new QuerySpecification<TestOrder>(filter)
            .AddInclude(o => o.Customer);

        var results = (await evaluator.EvaluateQueryAsync(spec)).ToList();

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(o => o.Customer.Should().NotBeNull());
    }

    [Fact]
    public async Task Include_ItemsNavigation_LoadsOrderItemsCollection()
    {
        await using var ctx = await CreateSeededWithRelationsAsync();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        var filter = new ValiFlowQuery<TestOrder>().EqualTo(o => o.Id, 2);
        var spec = new BasicSpecification<TestOrder>(filter)
            .AddInclude(o => o.Items);

        var result = await evaluator.EvaluateGetFirstAsync(spec);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().ProductName.Should().Be("Doohickey");
    }
}
