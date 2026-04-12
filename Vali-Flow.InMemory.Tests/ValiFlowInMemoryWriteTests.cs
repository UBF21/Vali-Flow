using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

public sealed class ValiFlowInMemoryWriteTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static List<TestProduct> CreateMutableSeed() => new()
    {
        new() { Id = 1,  Name = "Apple",  Category = "Fruit",  Price = 1.5m,  Stock = 100, IsActive = true  },
        new() { Id = 2,  Name = "Banana", Category = "Fruit",  Price = 0.8m,  Stock = 150, IsActive = true  },
        new() { Id = 3,  Name = "Carrot", Category = "Veggie", Price = 0.5m,  Stock = 200, IsActive = true  },
        new() { Id = 4,  Name = "Donut",  Category = "Sweet",  Price = 2.5m,  Stock = 50,  IsActive = false },
        new() { Id = 5,  Name = "Egg",    Category = "Dairy",  Price = 3.0m,  Stock = 80,  IsActive = true  },
        new() { Id = 6,  Name = "Fig",    Category = "Fruit",  Price = 4.5m,  Stock = 30,  IsActive = true  },
        new() { Id = 7,  Name = "Grape",  Category = "Fruit",  Price = 3.2m,  Stock = 60,  IsActive = false },
        new() { Id = 8,  Name = "Honey",  Category = "Sweet",  Price = 7.0m,  Stock = 20,  IsActive = true  },
        new() { Id = 9,  Name = "Ice",    Category = "Dairy",  Price = 1.2m,  Stock = 90,  IsActive = true  },
        new() { Id = 10, Name = "Jam",    Category = "Sweet",  Price = 2.8m,  Stock = 45,  IsActive = false },
    };

    private static ValiFlowEvaluator<TestProduct, int> CreateEvaluator(List<TestProduct> data)
        => new(data, null, p => p.Id);

    // -----------------------------------------------------------------------
    // Add
    // -----------------------------------------------------------------------

    [Fact]
    public void Add_NewEntity_ReturnsTrue()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProduct = new TestProduct { Id = 11, Name = "Kiwi", Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true };

        bool result = evaluator.Add(newProduct);

        result.Should().BeTrue();
    }

    [Fact]
    public void Add_NewEntityThenSaveChanges_EntityAppearsInStore()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProduct = new TestProduct { Id = 11, Name = "Kiwi", Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true };

        evaluator.Add(newProduct);
        evaluator.SaveChanges();

        int count = evaluator.EvaluateCount(null);
        count.Should().Be(11);
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 11))
            .Should().NotBeNull();
    }

    [Fact]
    public void Add_ToExplicitList_AppliedAfterSaveChanges()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProduct = new TestProduct { Id = 11, Name = "Kiwi", Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true };

        evaluator.Add(newProduct, data);

        // Deferred: list unchanged until SaveChanges
        data.Should().HaveCount(10);
        data.Should().NotContain(p => p.Id == 11);

        evaluator.SaveChanges(data);

        data.Should().HaveCount(11);
        data.Should().Contain(p => p.Id == 11);
    }

    // -----------------------------------------------------------------------
    // AddRange
    // -----------------------------------------------------------------------

    [Fact]
    public void AddRange_ThenSaveChanges_AllEntitiesAppearsInStore()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProducts = new List<TestProduct>
        {
            new() { Id = 11, Name = "Kiwi",  Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true },
            new() { Id = 12, Name = "Lemon", Category = "Fruit", Price = 1.1m, Stock = 85, IsActive = true },
        };

        evaluator.AddRange(newProducts);
        evaluator.SaveChanges();

        int count = evaluator.EvaluateCount(null);
        count.Should().Be(12);
    }

    [Fact]
    public void AddRange_ToExplicitList_AppliedAfterSaveChanges()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProducts = new List<TestProduct>
        {
            new() { Id = 11, Name = "Kiwi",  Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true },
            new() { Id = 12, Name = "Lemon", Category = "Fruit", Price = 1.1m, Stock = 85, IsActive = true },
        };

        evaluator.AddRange(newProducts, data);

        // Deferred: list unchanged until SaveChanges
        data.Should().HaveCount(10);

        evaluator.SaveChanges(data);

        data.Should().HaveCount(12);
    }

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    [Fact]
    public void Update_ExistingEntity_ReturnsMutatedEntity()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var updated = new TestProduct { Id = 1, Name = "Apple Pro", Category = "Fruit", Price = 2.0m, Stock = 110, IsActive = true };

        TestProduct? result = evaluator.Update(updated);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Apple Pro");
    }

    [Fact]
    public void Update_ExistingEntityThenSaveChanges_StoreReflectsChange()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var updated = new TestProduct { Id = 5, Name = "Egg Plus", Category = "Dairy", Price = 3.5m, Stock = 95, IsActive = true };

        evaluator.Update(updated);
        evaluator.SaveChanges();

        TestProduct? found = evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 5));
        found.Should().NotBeNull();
        found!.Name.Should().Be("Egg Plus");
        found.Price.Should().Be(3.5m);
    }

    [Fact]
    public void Update_NonExistingEntity_ReturnsNull()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var ghost = new TestProduct { Id = 999, Name = "Ghost", Category = "None", Price = 0m, Stock = 0, IsActive = false };

        TestProduct? result = evaluator.Update(ghost);

        result.Should().BeNull();
    }

    [Fact]
    public void Update_ToExplicitList_UpdatesListImmediately()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var updated = new TestProduct { Id = 2, Name = "Banana Split", Category = "Fruit", Price = 0.9m, Stock = 155, IsActive = true };

        evaluator.Update(updated, data);

        var inList = data.First(p => p.Id == 2);
        inList.Name.Should().Be("Banana Split");
    }

    // -----------------------------------------------------------------------
    // UpdateRange
    // -----------------------------------------------------------------------

    [Fact]
    public void UpdateRange_MultipleExistingEntities_ReturnsAllUpdated()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var toUpdate = new List<TestProduct>
        {
            new() { Id = 1, Name = "Apple NEW",  Category = "Fruit", Price = 1.6m, Stock = 101, IsActive = true },
            new() { Id = 2, Name = "Banana NEW", Category = "Fruit", Price = 0.9m, Stock = 151, IsActive = true },
        };

        IEnumerable<TestProduct> result = evaluator.UpdateRange(toUpdate);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateRange_ThenSaveChanges_StoreReflectsAllChanges()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var toUpdate = new List<TestProduct>
        {
            new() { Id = 8, Name = "Honey Gold", Category = "Sweet", Price = 8.0m, Stock = 25, IsActive = true },
            new() { Id = 9, Name = "Ice Blue",   Category = "Dairy", Price = 1.5m, Stock = 95, IsActive = true },
        };

        evaluator.UpdateRange(toUpdate);
        evaluator.SaveChanges();

        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 8))!.Name.Should().Be("Honey Gold");
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 9))!.Name.Should().Be("Ice Blue");
    }

    [Fact]
    public void UpdateRange_MixExistingAndNonExisting_OnlyExistingAreReturned()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var toUpdate = new List<TestProduct>
        {
            new() { Id = 3,   Name = "Carrot NEW", Category = "Veggie", Price = 0.6m, Stock = 205, IsActive = true },
            new() { Id = 999, Name = "Ghost",       Category = "None",   Price = 0m,   Stock = 0,   IsActive = false },
        };

        IEnumerable<TestProduct> result = evaluator.UpdateRange(toUpdate);

        result.Should().HaveCount(1);
        result.Single().Id.Should().Be(3);
    }

    // -----------------------------------------------------------------------
    // Delete
    // -----------------------------------------------------------------------

    [Fact]
    public void Delete_ExistingEntity_ReturnsTrue()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var target = data.First(p => p.Id == 4); // Donut

        bool result = evaluator.Delete(target);

        result.Should().BeTrue();
    }

    [Fact]
    public void Delete_ExistingEntityThenSaveChanges_StoreHasNineItems()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var target = data.First(p => p.Id == 4);

        evaluator.Delete(target);
        evaluator.SaveChanges();

        evaluator.EvaluateCount(null).Should().Be(9);
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 4)).Should().BeNull();
    }

    [Fact]
    public void Delete_NonExistingEntity_ReturnsFalse()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var ghost = new TestProduct { Id = 999 };

        bool result = evaluator.Delete(ghost);

        result.Should().BeFalse();
    }

    [Fact]
    public void Delete_ToExplicitList_RemovesFromListAfterSaveChanges()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var target = data.First(p => p.Id == 7); // Grape

        evaluator.Delete(target, data);

        // Deferred: list unchanged before SaveChanges
        data.Should().HaveCount(10);

        evaluator.SaveChanges(data);

        data.Should().HaveCount(9);
        data.Should().NotContain(p => p.Id == 7);
    }

    // -----------------------------------------------------------------------
    // DeleteRange
    // -----------------------------------------------------------------------

    [Fact]
    public void DeleteRange_TwoExistingEntities_ReturnsTwo()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var toDelete = data.Where(p => p.Id == 4 || p.Id == 7).ToList();

        int count = evaluator.DeleteRange(toDelete);

        count.Should().Be(2);
    }

    [Fact]
    public void DeleteRange_ThenSaveChanges_StoreHasEightItems()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var toDelete = data.Where(p => p.Id == 4 || p.Id == 10).ToList();

        evaluator.DeleteRange(toDelete);
        evaluator.SaveChanges();

        evaluator.EvaluateCount(null).Should().Be(8);
    }

    [Fact]
    public void DeleteRange_AllInactiveProducts_RemovesThree()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var inactive = data.Where(p => !p.IsActive).ToList();

        evaluator.DeleteRange(inactive);
        evaluator.SaveChanges();

        evaluator.EvaluateCount(null).Should().Be(7);
        evaluator.EvaluateCount(null, new ValiFlow<TestProduct>().IsFalse(p => p.IsActive)).Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // SaveChanges
    // -----------------------------------------------------------------------

    [Fact]
    public void SaveChanges_WithExplicitEntitiesArg_ClearsPendingChangesWithoutModifyingStore()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProduct = new TestProduct { Id = 11, Name = "Kiwi", Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true };

        evaluator.Add(newProduct);
        // Passing 'data' to SaveChanges signals "external list managed" — it clears the queue
        evaluator.SaveChanges(data);

        // Store was not touched; 'data' still has 10 items
        evaluator.EvaluateCount(null).Should().Be(10);
    }

    [Fact]
    public void SaveChanges_CalledTwice_SecondCallHasNoEffect()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProduct = new TestProduct { Id = 11, Name = "Kiwi", Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true };

        evaluator.Add(newProduct);
        evaluator.SaveChanges(); // first call — commits the add
        evaluator.SaveChanges(); // second call — no pending changes

        evaluator.EvaluateCount(null).Should().Be(11);
    }

    // ── AddRange (additional) ─────────────────────────────────────────────────

    [Fact]
    public void AddRange_EmptyList_NoChangeToStore()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        evaluator.AddRange(new List<TestProduct>());
        evaluator.SaveChanges();

        evaluator.EvaluateCount(null).Should().Be(10);
    }

    [Fact]
    public void AddRange_ThreeProducts_AllAddedAfterSave()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProducts = new List<TestProduct>
        {
            new() { Id = 11, Name = "Kiwi",       Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true },
            new() { Id = 12, Name = "Lemon",       Category = "Fruit", Price = 1.1m, Stock = 85, IsActive = true },
            new() { Id = 13, Name = "Mango",       Category = "Fruit", Price = 3.5m, Stock = 45, IsActive = true },
        };

        evaluator.AddRange(newProducts);
        evaluator.SaveChanges();

        evaluator.EvaluateCount(null).Should().Be(13);
    }

    // ── UpdateRange (additional) ──────────────────────────────────────────────

    [Fact]
    public void UpdateRange_EmptyList_NoChangeToStore()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        var result = evaluator.UpdateRange(new List<TestProduct>());
        evaluator.SaveChanges();

        result.Should().BeEmpty();
        evaluator.EvaluateCount(null).Should().Be(10);
    }

    // ── DeleteRange (additional) ──────────────────────────────────────────────

    [Fact]
    public void DeleteRange_EmptyList_ReturnsZero()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        int count = evaluator.DeleteRange(new List<TestProduct>());
        evaluator.SaveChanges();

        count.Should().Be(0);
        evaluator.EvaluateCount(null).Should().Be(10);
    }

    [Fact]
    public void DeleteRange_ToExplicitList_RemovesAfterSaveChanges()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var toDelete = data.Where(p => p.Id <= 3).ToList();

        evaluator.DeleteRange(toDelete, data);

        // Deferred: list unchanged before SaveChanges
        data.Should().HaveCount(10);

        evaluator.SaveChanges(data);

        data.Should().HaveCount(7);
    }

    // ── SaveChanges (additional) ──────────────────────────────────────────────

    [Fact]
    public void SaveChanges_AfterAddAndDelete_StoreIsConsistent()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        evaluator.Add(new TestProduct { Id = 11, Name = "Kiwi", Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true });
        var toDelete = data.First(p => p.Id == 1);
        evaluator.Delete(toDelete);
        evaluator.SaveChanges();

        // Added 1, deleted 1 → still 10
        evaluator.EvaluateCount(null).Should().Be(10);
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 11)).Should().NotBeNull();
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1)).Should().BeNull();
    }
}
