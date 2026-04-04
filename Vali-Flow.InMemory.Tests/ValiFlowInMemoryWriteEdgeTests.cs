using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;
using Xunit;

namespace Vali_Flow.InMemory.Tests;

/// <summary>Edge cases for write operations: DeleteRange filtering, deferred SaveChanges with mixed ops.</summary>
public sealed class ValiFlowInMemoryWriteEdgeTests
{
    private static List<TestProduct> MakeData() => new()
    {
        new() { Id = 1, Name = "A", Category = "X", Price = 10m, Stock = 1, IsActive = true  },
        new() { Id = 2, Name = "B", Category = "X", Price = 20m, Stock = 2, IsActive = false },
        new() { Id = 3, Name = "C", Category = "Y", Price = 30m, Stock = 3, IsActive = true  },
    };

    // ── DeleteRange ───────────────────────────────────────────────────────────

    [Fact]
    public void DeleteRange_MatchingFilter_RemovesOnlyMatchingEntities()
    {
        var data = MakeData();
        var ev = new ValiFlowEvaluator<TestProduct, int>(data);
        var toDelete = data.Where(p => p.IsActive).ToList();

        ev.DeleteRange(toDelete);
        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(1);
        ev.EvaluateAny(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 2)).Should().BeTrue();
    }

    [Fact]
    public void DeleteRange_EmptyList_NoChangeToStore()
    {
        var data = MakeData();
        var ev = new ValiFlowEvaluator<TestProduct, int>(data);

        ev.DeleteRange(new List<TestProduct>());
        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(3);
    }

    // ── SaveChanges — mixed deferred operations ───────────────────────────────

    [Fact]
    public void SaveChanges_DeferredAddAndDelete_BothApplied()
    {
        var data = MakeData();
        var ev = new ValiFlowEvaluator<TestProduct, int>(data);

        // Deferred add
        var newProduct = new TestProduct { Id = 99, Name = "New", Category = "Z", Price = 99m, Stock = 9, IsActive = true };
        ev.Add(newProduct);

        // Deferred delete
        ev.Delete(data.First(p => p.Id == 2));

        // Before save — internal state not yet committed
        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(3); // 3 original - 1 deleted + 1 added
        ev.EvaluateAny(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 99)).Should().BeTrue();
        ev.EvaluateAny(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 2)).Should().BeFalse();
    }

    [Fact]
    public void SaveChanges_MultipleAdds_AllPersisted()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>();

        ev.Add(new TestProduct { Id = 1, Name = "X", Category = "A", Price = 1m, Stock = 1, IsActive = true });
        ev.Add(new TestProduct { Id = 2, Name = "Y", Category = "A", Price = 2m, Stock = 1, IsActive = true });
        ev.Add(new TestProduct { Id = 3, Name = "Z", Category = "B", Price = 3m, Stock = 1, IsActive = true });

        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(3);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ExistingEntity_ReplacesInStore()
    {
        var data = MakeData();
        var ev = new ValiFlowEvaluator<TestProduct, int>(data);

        var updated = new TestProduct { Id = 1, Name = "A-updated", Category = "X", Price = 999m, Stock = 1, IsActive = true };
        ev.Update(updated);
        ev.SaveChanges();

        var result = ev.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1));
        result!.Name.Should().Be("A-updated");
        result.Price.Should().Be(999m);
    }
}
