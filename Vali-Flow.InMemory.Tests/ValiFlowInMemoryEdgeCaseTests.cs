using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;
using Xunit;

namespace Vali_Flow.InMemory.Tests;

/// <summary>
/// Covers: constructor reflection caching, non-List external store validation,
/// GetFirstFailed / EvaluateAllFailed negation semantics.
/// </summary>
public sealed class ValiFlowInMemoryEdgeCaseTests
{
    // ── Constructor — reflection caching ─────────────────────────────────────

    [Fact]
    public void Constructor_WithoutGetId_WorksWithIdProperty()
    {
        // Should not throw — TestProduct has an int Id property
        var ev = new ValiFlowEvaluator<TestProduct, int>();
        ev.EvaluateCount(null).Should().Be(0);
    }

    [Fact]
    public void Constructor_WithoutGetId_UsesIdPropertyForOperations()
    {
        var products = new List<TestProduct>
        {
            new() { Id = 1, Name = "Alpha", IsActive = true, Price = 10m, Stock = 5, Category = "A" },
            new() { Id = 2, Name = "Beta",  IsActive = false, Price = 20m, Stock = 3, Category = "B" }
        };
        // No getId provided — should use reflection-based fallback correctly
        var ev = new ValiFlowEvaluator<TestProduct, int>(products);

        ev.EvaluateCount(null).Should().Be(2);
    }

    [Fact]
    public void Constructor_EntityWithoutIdProperty_ThrowsInvalidOperationException()
    {
        // Use an anonymous-style class without Id; since we can't do that easily, test via a nested type
        Action act = () => new ValiFlowEvaluator<NoIdEntity, int>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NoIdEntity*");
    }

    // ── Add — non-List external store ─────────────────────────────────────────

    [Fact]
    public void Add_WithNonListExternalStore_ThrowsArgumentException()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>();
        var product = new TestProduct { Id = 1, Name = "X", IsActive = true, Price = 5m, Stock = 1, Category = "A" };
        IEnumerable<TestProduct> array = new TestProduct[] { product };

        Action act = () => ev.Add(product, array);

        act.Should().Throw<ArgumentException>().WithParameterName("entities");
    }

    [Fact]
    public void Add_WithListExternalStore_PersistsOnSaveChanges()
    {
        var externalList = new List<TestProduct>();
        var ev = new ValiFlowEvaluator<TestProduct, int>();
        var product = new TestProduct { Id = 1, Name = "X", IsActive = true, Price = 5m, Stock = 1, Category = "A" };

        ev.Add(product, externalList);
        ev.SaveChanges(externalList);

        externalList.Should().ContainSingle(p => p.Id == 1);
    }

    // ── AddRange — non-List external store ───────────────────────────────────

    [Fact]
    public void AddRange_WithNonListExternalStore_ThrowsArgumentException()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>();
        var products = new[]
        {
            new TestProduct { Id = 1, Name = "X", IsActive = true, Price = 5m, Stock = 1, Category = "A" }
        };

        Action act = () => ev.AddRange(products, (IEnumerable<TestProduct>)products);

        act.Should().Throw<ArgumentException>().WithParameterName("entities");
    }

    [Fact]
    public void AddRange_WithListExternalStore_AddsAllEntities()
    {
        var externalList = new List<TestProduct>();
        var ev = new ValiFlowEvaluator<TestProduct, int>();
        var products = new[]
        {
            new TestProduct { Id = 1, Name = "A", IsActive = true,  Price = 5m,  Stock = 1, Category = "X" },
            new TestProduct { Id = 2, Name = "B", IsActive = false, Price = 10m, Stock = 2, Category = "X" }
        };

        ev.AddRange(products, externalList);
        ev.SaveChanges(externalList);

        externalList.Should().HaveCount(2);
    }

    // ── Upsert — non-List external store ─────────────────────────────────────

    [Fact]
    public void Upsert_WithNonListExternalStore_ThrowsArgumentException()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>();
        var product = new TestProduct { Id = 1, Name = "X", IsActive = true, Price = 5m, Stock = 1, Category = "A" };
        IEnumerable<TestProduct> array = new[] { product };

        Action act = () => ev.Upsert(product, array);

        act.Should().Throw<ArgumentException>().WithParameterName("entities");
    }

    // ── GetFirstFailed — negation semantics ──────────────────────────────────

    [Fact]
    public void GetFirstFailed_DefaultNegate_ReturnsEntityThatFailsFilter()
    {
        var products = new List<TestProduct>
        {
            new() { Id = 1, Name = "A", IsActive = true,  Price = 10m, Stock = 1, Category = "X" },
            new() { Id = 2, Name = "B", IsActive = false, Price = 20m, Stock = 2, Category = "X" }
        };
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var ev = new ValiFlowEvaluator<TestProduct, int>(products, filter);

        // GetFirstFailed(negateCondition: false) → entity that does NOT satisfy IsActive
        var result = ev.GetFirstFailed(products);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GetFirstFailed_NegateConditionTrue_ReturnsEntityThatPassesFilter()
    {
        var products = new List<TestProduct>
        {
            new() { Id = 1, Name = "A", IsActive = true,  Price = 10m, Stock = 1, Category = "X" },
            new() { Id = 2, Name = "B", IsActive = false, Price = 20m, Stock = 2, Category = "X" }
        };
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var ev = new ValiFlowEvaluator<TestProduct, int>(products, filter);

        // negateCondition: true inverts the "failed" logic → returns first PASSING entity
        var result = ev.GetFirstFailed(products, negateCondition: true);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
    }

    // ── EvaluateAllFailed ─────────────────────────────────────────────────────

    [Fact]
    public void EvaluateAllFailed_DefaultNegate_OnlyReturnsFailingEntities()
    {
        var products = new List<TestProduct>
        {
            new() { Id = 1, Name = "A", IsActive = true,  Price = 10m, Stock = 1, Category = "X" },
            new() { Id = 2, Name = "B", IsActive = false, Price = 20m, Stock = 2, Category = "X" },
            new() { Id = 3, Name = "C", IsActive = false, Price = 30m, Stock = 3, Category = "X" }
        };
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var ev = new ValiFlowEvaluator<TestProduct, int>(products, filter);

        var failed = ev.EvaluateAllFailed<int>(products).ToList();

        failed.Should().HaveCount(2);
        failed.Should().OnlyContain(p => !p.IsActive);
    }

    // ── SaveChanges — internal store consistency ──────────────────────────────

    [Fact]
    public void SaveChanges_AddUpdateDelete_InternalStoreConsistent()
    {
        var initial = new List<TestProduct>
        {
            new() { Id = 1, Name = "A", IsActive = true,  Price = 10m, Stock = 1, Category = "X" },
            new() { Id = 2, Name = "B", IsActive = false, Price = 20m, Stock = 2, Category = "X" }
        };
        var ev = new ValiFlowEvaluator<TestProduct, int>(initial);

        var newProduct = new TestProduct { Id = 3, Name = "C", IsActive = true, Price = 30m, Stock = 3, Category = "X" };
        ev.Add(newProduct);

        var updated = new TestProduct { Id = 1, Name = "A-updated", IsActive = true, Price = 15m, Stock = 1, Category = "X" };
        ev.Update(updated);

        var toDelete = initial.First(p => p.Id == 2);
        ev.Delete(toDelete);

        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(2); // original 2 - 1 deleted + 1 added
        var first = ev.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1));
        first!.Name.Should().Be("A-updated");
        ev.EvaluateAny(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 2)).Should().BeFalse();
        ev.EvaluateAny(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 3)).Should().BeTrue();
    }
}

/// <summary>Helper type without an Id property, used to verify the constructor exception.</summary>
internal sealed class NoIdEntity
{
    public int Code { get; set; }
}
