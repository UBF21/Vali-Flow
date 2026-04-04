using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

public sealed class ValiFlowInMemoryNewMethodTests
{
    // Helpers

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

    // ── Upsert ────────────────────────────────────────────────────────────────

    [Fact]
    public void Upsert_NewEntity_InsertsAndReturnsEntity()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProduct = new TestProduct { Id = 99, Name = "Kiwi", Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true };

        var result = evaluator.Upsert(newProduct);
        evaluator.SaveChanges();

        result.Should().NotBeNull();
        result.Id.Should().Be(99);
        evaluator.EvaluateCount(null).Should().Be(11);
    }

    [Fact]
    public void Upsert_ExistingEntity_UpdatesInStore()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var updated = new TestProduct { Id = 1, Name = "Apple Pro", Category = "Fruit", Price = 9.9m, Stock = 999, IsActive = true };

        evaluator.Upsert(updated);
        evaluator.SaveChanges();

        var found = evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1));
        found.Should().NotBeNull();
        found!.Name.Should().Be("Apple Pro");
        found.Price.Should().Be(9.9m);
    }

    [Fact]
    public void Upsert_ToExplicitList_ImmediatelyUpdatesNewEntities()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newProduct = new TestProduct { Id = 50, Name = "Mango", Category = "Fruit", Price = 3.0m, Stock = 40, IsActive = true };

        evaluator.Upsert(newProduct, data);

        data.Should().HaveCount(11);
        data.Should().Contain(p => p.Id == 50);
    }

    [Fact]
    public void Upsert_ToExplicitList_ExistingIsUpdatedInList()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var updated = new TestProduct { Id = 3, Name = "Carrot XL", Category = "Veggie", Price = 0.9m, Stock = 210, IsActive = true };

        evaluator.Upsert(updated, data);

        var inList = data.First(p => p.Id == 3);
        inList.Name.Should().Be("Carrot XL");
    }

    // ── UpsertRange ───────────────────────────────────────────────────────────

    [Fact]
    public void UpsertRange_EmptyList_NoChange()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        var result = evaluator.UpsertRange(new List<TestProduct>());
        evaluator.SaveChanges();

        result.Should().BeEmpty();
        evaluator.EvaluateCount(null).Should().Be(10);
    }

    [Fact]
    public void UpsertRange_AllNew_InsertsAll()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var newItems = new List<TestProduct>
        {
            new() { Id = 11, Name = "Kiwi",  Category = "Fruit", Price = 2.0m, Stock = 70, IsActive = true },
            new() { Id = 12, Name = "Lemon", Category = "Fruit", Price = 1.1m, Stock = 55, IsActive = true },
        };

        evaluator.UpsertRange(newItems);
        evaluator.SaveChanges();

        evaluator.EvaluateCount(null).Should().Be(12);
    }

    [Fact]
    public void UpsertRange_AllExisting_UpdatesAll()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var toUpdate = new List<TestProduct>
        {
            new() { Id = 1, Name = "Apple v2",  Category = "Fruit", Price = 2.0m, Stock = 101, IsActive = true },
            new() { Id = 2, Name = "Banana v2", Category = "Fruit", Price = 1.0m, Stock = 151, IsActive = true },
        };

        evaluator.UpsertRange(toUpdate);
        evaluator.SaveChanges();

        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1))!.Name.Should().Be("Apple v2");
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 2))!.Name.Should().Be("Banana v2");
    }

    [Fact]
    public void UpsertRange_MixedNewAndExisting_CorrectInsertAndUpdate()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);
        var mixed = new List<TestProduct>
        {
            new() { Id = 1,  Name = "Apple UPDATED", Category = "Fruit", Price = 5.0m, Stock = 100, IsActive = true }, // existing
            new() { Id = 99, Name = "Papaya",         Category = "Fruit", Price = 4.0m, Stock = 25,  IsActive = true }, // new
        };

        evaluator.UpsertRange(mixed);
        evaluator.SaveChanges();

        evaluator.EvaluateCount(null).Should().Be(11);
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1))!.Name.Should().Be("Apple UPDATED");
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 99)).Should().NotBeNull();
    }

    // ── DeleteByCondition ─────────────────────────────────────────────────────

    [Fact]
    public void DeleteByCondition_NoMatch_ReturnsZero()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        int deleted = evaluator.DeleteByCondition(p => p.Price > 1000m);
        evaluator.SaveChanges();

        deleted.Should().Be(0);
        evaluator.EvaluateCount(null).Should().Be(10);
    }

    [Fact]
    public void DeleteByCondition_SingleMatch_DeletesOne()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        int deleted = evaluator.DeleteByCondition(p => p.Id == 5);
        evaluator.SaveChanges();

        deleted.Should().Be(1);
        evaluator.EvaluateCount(null).Should().Be(9);
        evaluator.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 5)).Should().BeNull();
    }

    [Fact]
    public void DeleteByCondition_MultipleMatches_DeletesAll()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        // 3 inactive: Id 4, 7, 10
        int deleted = evaluator.DeleteByCondition(p => !p.IsActive);
        evaluator.SaveChanges();

        deleted.Should().Be(3);
        evaluator.EvaluateCount(null).Should().Be(7);
    }

    [Fact]
    public void DeleteByCondition_ToExplicitList_RemovesImmediately()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        int deleted = evaluator.DeleteByCondition(p => p.Category == "Sweet", data);

        // Sweet: Donut(4), Honey(8), Jam(10) = 3
        deleted.Should().Be(3);
        data.Should().HaveCount(7);
        data.Should().NotContain(p => p.Category == "Sweet");
    }

    // ── EvaluatePagedResult ───────────────────────────────────────────────────

    [Fact]
    public void EvaluatePagedResult_FirstPage_CorrectMetadata()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        var result = evaluator.EvaluatePagedResult<int>(data, page: 1, pageSize: 3);

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().Be(10);
        result.TotalPages.Should().Be(4); // ceil(10/3)=4
        result.Items.Should().HaveCount(3);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void EvaluatePagedResult_LastPage_CorrectItemCount()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        var result = evaluator.EvaluatePagedResult<int>(data, page: 4, pageSize: 3);

        result.Page.Should().Be(4);
        result.Items.Should().HaveCount(1); // 10 items, page 4 of 3 → only 1 remaining
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void EvaluatePagedResult_WithOrdering_ItemsAreSorted()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        var result = evaluator.EvaluatePagedResult<decimal>(data, page: 1, pageSize: 5, orderBy: p => p.Price, ascending: true);

        result.Items.Should().HaveCount(5);
        result.Items[0].Price.Should().BeLessOrEqualTo(result.Items[1].Price);
        result.Items[1].Price.Should().BeLessOrEqualTo(result.Items[2].Price);
    }

    [Fact]
    public void EvaluatePagedResult_InvalidPage_ThrowsArgumentException()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        Action act = () => evaluator.EvaluatePagedResult<int>(data, page: 0, pageSize: 5);

        act.Should().Throw<ArgumentException>();
    }

    // ── EvaluateDuplicates ────────────────────────────────────────────────────

    [Fact]
    public void EvaluateDuplicates_NoDuplicates_ReturnsEmpty()
    {
        var data = CreateMutableSeed(); // all unique IDs
        var evaluator = CreateEvaluator(data);

        var result = evaluator.EvaluateDuplicates<int>(data, p => p.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateDuplicates_ByCategory_ReturnsDuplicates()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        // Fruit: 4 items, Veggie: 1, Sweet: 3, Dairy: 2
        // Duplicates by Category → all except unique ones
        var result = evaluator.EvaluateDuplicates<string>(data, p => p.Category).ToList();

        // All categories except the one with only 1 item (Veggie) have duplicates
        result.Should().NotBeEmpty();
        result.Should().NotContain(p => p.Category == "Veggie");
    }

    [Fact]
    public void EvaluateDuplicates_WithValiFlowFilter_OnlyDuplicatesFromFiltered()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        // Only active products, duplicates by category
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);
        var result = evaluator.EvaluateDuplicates<string>(data, p => p.Category, valiFlow: filter).ToList();

        result.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public void EvaluateDuplicates_WithOrdering_ResultsOrdered()
    {
        var data = CreateMutableSeed();
        var evaluator = CreateEvaluator(data);

        var result = evaluator.EvaluateDuplicates<string>(
            data,
            selector: p => p.Category,
            orderBy: p => p.Category,
            ascending: true
        ).ToList();

        if (result.Count >= 2)
        {
            string.Compare(result[0].Category, result[1].Category, StringComparison.Ordinal).Should().BeLessOrEqualTo(0);
        }
    }
}
