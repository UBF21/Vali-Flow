using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

public sealed class ValiFlowInMemoryUncoveredTests
{
    // -----------------------------------------------------------------------
    // Data: 5 products — 2 Electronics (active), 2 Books (1 active, 1 inactive),
    //       1 Clothing (inactive)
    // -----------------------------------------------------------------------
    private static List<TestProduct> MakeData() => new()
    {
        new() { Id = 1, Name = "A1", Category = "Electronics", Price = 100m, Stock = 5,  IsActive = true  },
        new() { Id = 2, Name = "A2", Category = "Electronics", Price = 200m, Stock = 3,  IsActive = true  },
        new() { Id = 3, Name = "B1", Category = "Books",       Price = 30m,  Stock = 10, IsActive = true  },
        new() { Id = 4, Name = "B2", Category = "Books",       Price = 50m,  Stock = 8,  IsActive = false },
        new() { Id = 5, Name = "C1", Category = "Clothing",    Price = 80m,  Stock = 2,  IsActive = false },
    };

    private static ValiFlowEvaluator<TestProduct, int> CreateEvaluator(List<TestProduct>? data = null)
        => new(data ?? MakeData(), null, p => p.Id);

    // -----------------------------------------------------------------------
    // EvaluatePaged<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluatePaged_Page1_ReturnsFirstPageItems()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePaged<int>(data, page: 1, pageSize: 2, orderBy: p => p.Id).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public void EvaluatePaged_Page2_ReturnsNextBatch()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePaged<int>(data, page: 2, pageSize: 2, orderBy: p => p.Id).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(4);
    }

    [Fact]
    public void EvaluatePaged_LastPage_ReturnsRemainingItems()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePaged<int>(data, page: 3, pageSize: 2, orderBy: p => p.Id).ToList();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(5);
    }

    [Fact]
    public void EvaluatePaged_WithFilter_ReturnsOnlyMatchingItems()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.EvaluatePaged<int>(data, page: 1, pageSize: 10, orderBy: p => p.Id, valiFlow: filter).ToList();

        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public void EvaluatePaged_PageZero_ThrowsArgumentOutOfRangeException()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        Action act = () => evaluator.EvaluatePaged<int>(data, page: 0, pageSize: 2).ToList();

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("page");
    }

    [Fact]
    public void EvaluatePaged_PageSizeZero_ThrowsArgumentOutOfRangeException()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        Action act = () => evaluator.EvaluatePaged<int>(data, page: 1, pageSize: 0).ToList();

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }

    // -----------------------------------------------------------------------
    // EvaluatePagedResult<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluatePagedResult_ReturnsCorrectTotalCount()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePagedResult<int>(data, page: 1, pageSize: 2, orderBy: p => p.Id);

        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public void EvaluatePagedResult_ReturnsCorrectTotalPages()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePagedResult<int>(data, page: 1, pageSize: 2, orderBy: p => p.Id);

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void EvaluatePagedResult_FirstPage_HasNextPageTrueAndHasPreviousPageFalse()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePagedResult<int>(data, page: 1, pageSize: 2, orderBy: p => p.Id);

        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void EvaluatePagedResult_LastPage_HasNextPageFalseAndHasPreviousPageTrue()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePagedResult<int>(data, page: 3, pageSize: 2, orderBy: p => p.Id);

        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void EvaluatePagedResult_MiddlePage_HasNextAndPreviousPageTrue()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePagedResult<int>(data, page: 2, pageSize: 2, orderBy: p => p.Id);

        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void EvaluatePagedResult_ItemsMatchCorrectPage()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluatePagedResult<int>(data, page: 2, pageSize: 2, orderBy: p => p.Id);

        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be(3);
        result.Items[1].Id.Should().Be(4);
    }

    [Fact]
    public void EvaluatePagedResult_PageZero_ThrowsArgumentOutOfRangeException()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        Action act = () => evaluator.EvaluatePagedResult<int>(data, page: 0, pageSize: 2);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("page");
    }

    [Fact]
    public void EvaluatePagedResult_PageSizeZero_ThrowsArgumentOutOfRangeException()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        Action act = () => evaluator.EvaluatePagedResult<int>(data, page: 1, pageSize: 0);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("pageSize");
    }

    // -----------------------------------------------------------------------
    // EvaluateTop<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateTop_ReturnsTopNItems()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluateTop<int>(data, count: 3, orderBy: p => p.Id).ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void EvaluateTop_RespectsOrderingAscending()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluateTop<decimal>(data, count: 2, orderBy: p => p.Price, ascending: true).ToList();

        result[0].Price.Should().Be(30m);
        result[1].Price.Should().Be(50m);
    }

    [Fact]
    public void EvaluateTop_RespectsOrderingDescending()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluateTop<decimal>(data, count: 2, orderBy: p => p.Price, ascending: false).ToList();

        result[0].Price.Should().Be(200m);
        result[1].Price.Should().Be(100m);
    }

    [Fact]
    public void EvaluateTop_CountZero_ThrowsArgumentOutOfRangeException()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        Action act = () => evaluator.EvaluateTop<int>(data, count: 0).ToList();

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
    }

    [Fact]
    public void EvaluateTop_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        Action act = () => evaluator.EvaluateTop<int>(data, count: -1).ToList();

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
    }

    [Fact]
    public void EvaluateTop_WithFilter_ReturnsOnlyMatchingTopItems()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.EvaluateTop<int>(data, count: 2, orderBy: p => p.Id, valiFlow: filter).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.IsActive);
    }

    // -----------------------------------------------------------------------
    // EvaluateDistinct<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateDistinct_ByCategory_ReturnsOnePerCategory()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluateDistinct<string>(data, selector: p => p.Category).ToList();

        result.Should().HaveCount(3);
        result.Select(p => p.Category).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void EvaluateDistinct_ByCategory_PicksFirstOfEachGroup()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        var result = evaluator.EvaluateDistinct<string>(data, selector: p => p.Category, orderBy: p => p.Category).ToList();

        // Books -> first is Id=3 (B1), Electronics -> first is Id=1 (A1), Clothing -> first is Id=5 (C1)
        var categories = result.Select(p => p.Category).ToList();
        categories.Should().Contain("Electronics");
        categories.Should().Contain("Books");
        categories.Should().Contain("Clothing");
    }

    [Fact]
    public void EvaluateDistinct_NullSelector_ThrowsArgumentNullException()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        Action act = () => evaluator.EvaluateDistinct<string>(data, selector: null!).ToList();

        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void EvaluateDistinct_WithFilter_OnlyFiltersBeforeDistinct()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Only active products: Electronics(1,2), Books(3) — 2 distinct categories
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.EvaluateDistinct<string>(data, selector: p => p.Category, valiFlow: filter).ToList();

        result.Should().HaveCount(2);
        result.Select(p => p.Category).Should().Contain("Electronics").And.Contain("Books");
    }

    // -----------------------------------------------------------------------
    // EvaluateDuplicates<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateDuplicates_ByCategory_ReturnsAllDuplicateItems()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        // Electronics(1,2) and Books(3,4) have duplicates; Clothing(5) is unique
        var result = evaluator.EvaluateDuplicates<string>(data, selector: p => p.Category).ToList();

        result.Should().HaveCount(4);
        result.Should().NotContain(p => p.Category == "Clothing");
    }

    [Fact]
    public void EvaluateDuplicates_UniqueKeys_ReturnsEmpty()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        // All Ids are unique, no duplicates
        var result = evaluator.EvaluateDuplicates<int>(data, selector: p => p.Id).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateDuplicates_NullSelector_ThrowsArgumentNullException()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();

        Action act = () => evaluator.EvaluateDuplicates<string>(data, selector: null!).ToList();

        act.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void EvaluateDuplicates_WithFilter_OnlyConsidersMatchingItems()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Active products: Id=1 (Electronics), Id=2 (Electronics), Id=3 (Books)
        // Electronics still has 2 -> duplicates; Books only 1 active -> no duplicate
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.EvaluateDuplicates<string>(data, selector: p => p.Category, valiFlow: filter).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Category == "Electronics");
    }

    // -----------------------------------------------------------------------
    // GetFirstMatchIndex<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void GetFirstMatchIndex_MatchingCondition_ReturnsCorrectIndex()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Ordered by Id ascending: [1,2,3,4,5]. First inactive is Id=4 -> index 3
        var filter = new ValiFlow<TestProduct>().IsFalse(p => p.IsActive);

        int index = evaluator.GetFirstMatchIndex<int>(data, orderBy: p => p.Id, valiFlow: filter);

        index.Should().Be(3);
    }

    [Fact]
    public void GetFirstMatchIndex_NoMatch_ReturnsMinusOne()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Filter that matches nothing
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Price, 9999m);

        int index = evaluator.GetFirstMatchIndex<int>(data, orderBy: p => p.Id, valiFlow: filter);

        index.Should().Be(-1);
    }

    [Fact]
    public void GetFirstMatchIndex_FirstItemMatches_ReturnsZero()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Ordered by Id ascending: Id=1 is active -> index 0
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        int index = evaluator.GetFirstMatchIndex<int>(data, orderBy: p => p.Id, valiFlow: filter);

        index.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // GetLastMatchIndex<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void GetLastMatchIndex_MatchingCondition_ReturnsCorrectIndex()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Ordered by Id ascending: [1,2,3,4,5]. Last inactive is Id=5 -> index 4
        var filter = new ValiFlow<TestProduct>().IsFalse(p => p.IsActive);

        int index = evaluator.GetLastMatchIndex<int>(data, orderBy: p => p.Id, valiFlow: filter);

        index.Should().Be(4);
    }

    [Fact]
    public void GetLastMatchIndex_NoMatch_ReturnsMinusOne()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Price, 9999m);

        int index = evaluator.GetLastMatchIndex<int>(data, orderBy: p => p.Id, valiFlow: filter);

        index.Should().Be(-1);
    }

    [Fact]
    public void GetLastMatchIndex_LastItemMatches_ReturnsLastIndex()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Ordered by Id ascending: Id=3 is last active (Ids 4,5 are inactive) -> index 2
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        int index = evaluator.GetLastMatchIndex<int>(data, orderBy: p => p.Id, valiFlow: filter);

        index.Should().Be(2);
    }

    // -----------------------------------------------------------------------
    // GetLast<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void GetLast_WithFilter_ReturnsLastMatchingEntity()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active products ordered by Id: 1, 2, 3 — last is Id=3
        var result = evaluator.GetLast<int>(data, orderBy: p => p.Id, valiFlow: filter);

        result.Should().NotBeNull();
        result!.Id.Should().Be(3);
    }

    [Fact]
    public void GetLast_NoMatch_ReturnsNull()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        var filter = new ValiFlow<TestProduct>().GreaterThan(p => p.Price, 9999m);

        var result = evaluator.GetLast<int>(data, orderBy: p => p.Id, valiFlow: filter);

        result.Should().BeNull();
    }

    [Fact]
    public void GetLast_OrderedDescending_ReturnsLastInDescendingOrder()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        // Active products ordered by Id descending: 3, 2, 1 — last is Id=1
        var result = evaluator.GetLast<int>(data, orderBy: p => p.Id, ascending: false, valiFlow: filter);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // GetLastFailed<TKey>
    // -----------------------------------------------------------------------

    [Fact]
    public void GetLastFailed_WithFilter_ReturnsLastNonMatchingEntity()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // GetLastFailed with IsActive filter returns the last entity where IsActive == false
        // Inactive: Id=4, Id=5 — ordered by Id asc, last inactive is Id=5
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.GetLastFailed<int>(data, orderBy: p => p.Id, valiFlow: filter);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
        result.Id.Should().Be(5);
    }

    [Fact]
    public void GetLastFailed_NoFailures_ReturnsNull()
    {
        var evaluator = CreateEvaluator();
        // All products match => no "failed" items
        var allActiveData = MakeData().Where(p => p.IsActive).ToList();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.GetLastFailed<int>(allActiveData, orderBy: p => p.Id, valiFlow: filter);

        result.Should().BeNull();
    }

    [Fact]
    public void GetLastFailed_WithNegateCondition_ReturnsLastMatchingInstead()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // negateCondition=true inverts the Failed logic so it returns last entity matching the condition
        // Active products ordered by Id asc: 1,2,3 — last is Id=3
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.GetLastFailed<int>(data, orderBy: p => p.Id, valiFlow: filter, negateCondition: true);

        result.Should().NotBeNull();
        result!.Id.Should().Be(3);
    }

    // -----------------------------------------------------------------------
    // DeleteByCondition
    // -----------------------------------------------------------------------

    [Fact]
    public void DeleteByCondition_MatchingItems_DeletesAndReturnsCount()
    {
        var data = MakeData();
        var evaluator = CreateEvaluator(data);

        int deleted = evaluator.DeleteByCondition(p => p.IsActive == false, data);

        deleted.Should().Be(2);
        data.Should().HaveCount(3);
        data.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public void DeleteByCondition_NoMatches_ReturnsZeroAndDataUnchanged()
    {
        var data = MakeData();
        var evaluator = CreateEvaluator(data);

        int deleted = evaluator.DeleteByCondition(p => p.Price > 9999m, data);

        deleted.Should().Be(0);
        data.Should().HaveCount(5);
    }

    [Fact]
    public void DeleteByCondition_AllMatches_DeletesAll()
    {
        var data = MakeData();
        var evaluator = CreateEvaluator(data);

        int deleted = evaluator.DeleteByCondition(p => p.Id > 0, data);

        deleted.Should().Be(5);
        data.Should().BeEmpty();
    }

    [Fact]
    public void DeleteByCondition_SingleMatch_DeletesCorrectItem()
    {
        var data = MakeData();
        var evaluator = CreateEvaluator(data);

        int deleted = evaluator.DeleteByCondition(p => p.Id == 3, data);

        deleted.Should().Be(1);
        data.Should().HaveCount(4);
        data.Should().NotContain(p => p.Id == 3);
    }

    // -----------------------------------------------------------------------
    // negateCondition in EvaluateCountByGroup
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateCountByGroup_WithNegateCondition_CountsNegatedGroup()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // negateCondition=true with IsActive filter -> counts inactive products by category
        // Inactive: Id=4 (Books), Id=5 (Clothing) => Books:1, Clothing:1
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.EvaluateCountByGroup<string>(data, keySelector: p => p.Category, valiFlow: filter, negateCondition: true);

        result.Should().HaveCount(2);
        result["Books"].Should().Be(1);
        result["Clothing"].Should().Be(1);
        result.Should().NotContainKey("Electronics");
    }

    [Fact]
    public void EvaluateCountByGroup_WithoutNegateCondition_CountsActiveGroup()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Active: Id=1 (Electronics), Id=2 (Electronics), Id=3 (Books) => Electronics:2, Books:1
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.EvaluateCountByGroup<string>(data, keySelector: p => p.Category, valiFlow: filter);

        result.Should().HaveCount(2);
        result["Electronics"].Should().Be(2);
        result["Books"].Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // EvaluateGrouped with negateCondition: true
    // -----------------------------------------------------------------------

    [Fact]
    public void EvaluateGrouped_WithNegateCondition_ReturnsOnlyInactiveProducts()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // negateCondition=true inverts the IsActive filter -> groups only inactive products
        // Inactive: Id=4 (Books), Id=5 (Clothing)
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.EvaluateGrouped<string>(data, keySelector: p => p.Category, valiFlow: filter, negateCondition: true);

        result.Should().HaveCount(2);
        result.Should().ContainKey("Books");
        result.Should().ContainKey("Clothing");
        result["Books"].Should().HaveCount(1);
        result["Books"][0].Id.Should().Be(4);
        result["Clothing"].Should().HaveCount(1);
        result["Clothing"][0].Id.Should().Be(5);
    }

    [Fact]
    public void EvaluateGrouped_WithoutNegateCondition_ReturnsOnlyActiveProducts()
    {
        var evaluator = CreateEvaluator();
        var data = MakeData();
        // Active: Id=1 (Electronics), Id=2 (Electronics), Id=3 (Books)
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        var result = evaluator.EvaluateGrouped<string>(data, keySelector: p => p.Category, valiFlow: filter);

        result.Should().HaveCount(2);
        result.Should().ContainKey("Electronics");
        result.Should().ContainKey("Books");
        result["Electronics"].Should().HaveCount(2);
        result["Books"].Should().HaveCount(1);
        result.Should().NotContainKey("Clothing");
    }
}
