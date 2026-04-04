using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;
using Xunit;

namespace Vali_Flow.InMemory.Tests;

/// <summary>Tests for remaining uncovered paths in ValiFlowEvaluator (InMemory).</summary>
public sealed class ValiFlowInMemoryRemainingGapsTests
{
    // ── Helper entity without Id property ────────────────────────────────────

    private sealed class NoIdEntity
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // ── Helper entity with float/double fields ────────────────────────────────

    private sealed class MeasurementEntity
    {
        public int Id { get; set; }
        public float Weight { get; set; }
        public double Score { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private static ValiFlowEvaluator<TestProduct, int> MakeEvaluator(
        IEnumerable<TestProduct> data, ValiFlow<TestProduct>? valiFlow = null)
        => new(data, valiFlow, p => p.Id);

    private static List<TestProduct> MakeProducts() =>
    [
        new() { Id = 1, Name = "A", Category = "Electronics", Price = 100m, Stock = 5,  IsActive = true  },
        new() { Id = 2, Name = "B", Category = "Electronics", Price = 200m, Stock = 3,  IsActive = false },
        new() { Id = 3, Name = "C", Category = "Books",       Price = 40m,  Stock = 10, IsActive = true  },
        new() { Id = 4, Name = "D", Category = "Books",       Price = 60m,  Stock = 8,  IsActive = false },
    ];

    // ── Constructor — entity without Id property ──────────────────────────────

    [Fact]
    public void Constructor_EntityWithoutIdProperty_AndNoGetId_ThrowsInvalidOperationException()
    {
        var act = () => new ValiFlowEvaluator<NoIdEntity, int>();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NoIdEntity*");
    }

    [Fact]
    public void Constructor_EntityWithoutIdProperty_WithExplicitGetId_Succeeds()
    {
        var act = () => new ValiFlowEvaluator<NoIdEntity, int>(getId: e => e.Code);
        act.Should().NotThrow();
    }

    // ── GetFirstMatchIndex — negateCondition = true ───────────────────────────

    [Fact]
    public void GetFirstMatchIndex_NegateCondition_ReturnsFirstFailingIndex()
    {
        var data = MakeProducts();
        var filter = new ValiFlow<TestProduct>().Add(p => p.IsActive);
        var ev = MakeEvaluator(data, filter);

        // Without negate: first ACTIVE  → index 0 (Id=1)
        // With negate:    first INACTIVE → index 1 (Id=2) after default ordering
        int idx = ev.GetFirstMatchIndex<int>(null, negateCondition: true);

        idx.Should().Be(1);
    }

    [Fact]
    public void GetFirstMatchIndex_NegateCondition_NoMatch_ReturnsMinusOne()
    {
        var data = MakeProducts().Where(p => p.IsActive).ToList();
        var filter = new ValiFlow<TestProduct>().Add(p => p.IsActive);
        var ev = MakeEvaluator(data, filter);

        // All items are active, so negated (inactive) matches none
        int idx = ev.GetFirstMatchIndex<int>(null, negateCondition: true);

        idx.Should().Be(-1);
    }

    // ── GetLastMatchIndex — negateCondition = true ────────────────────────────

    [Fact]
    public void GetLastMatchIndex_NegateCondition_ReturnsLastFailingIndex()
    {
        var data = MakeProducts();
        var filter = new ValiFlow<TestProduct>().Add(p => p.IsActive);
        var ev = MakeEvaluator(data, filter);

        // Without negate: last ACTIVE  → index 2 (Id=3)
        // With negate:    last INACTIVE → index 3 (Id=4)
        int idx = ev.GetLastMatchIndex<int>(null, negateCondition: true);

        idx.Should().Be(3);
    }

    [Fact]
    public void GetLastMatchIndex_NegateCondition_NoMatch_ReturnsMinusOne()
    {
        // All items active → negateCondition=true means "look for inactive" → none found
        var data = MakeProducts().Where(p => p.IsActive).ToList();
        var filter = new ValiFlow<TestProduct>().Add(p => p.IsActive);
        var ev = MakeEvaluator(data, filter);

        int idx = ev.GetLastMatchIndex<int>(null, negateCondition: true);

        idx.Should().Be(-1);
    }

    // ── EvaluateSum — float selector ─────────────────────────────────────────

    [Fact]
    public void EvaluateSum_FloatSelector_ReturnsCorrectSum()
    {
        var data = new List<MeasurementEntity>
        {
            new() { Id = 1, Weight = 1.5f, IsActive = true  },
            new() { Id = 2, Weight = 2.5f, IsActive = true  },
            new() { Id = 3, Weight = 3.0f, IsActive = false },
        };
        var ev = new ValiFlowEvaluator<MeasurementEntity, int>(data, getId: e => e.Id);

        float sum = ev.EvaluateSum(null, e => e.Weight);

        sum.Should().BeApproximately(7.0f, 0.001f);
    }

    [Fact]
    public void EvaluateSum_FloatSelector_WithFilter_SumsOnlyMatchingItems()
    {
        var data = new List<MeasurementEntity>
        {
            new() { Id = 1, Weight = 1.5f, IsActive = true  },
            new() { Id = 2, Weight = 2.5f, IsActive = true  },
            new() { Id = 3, Weight = 3.0f, IsActive = false },
        };
        var filter = new ValiFlow<MeasurementEntity>().Add(e => e.IsActive);
        var ev = new ValiFlowEvaluator<MeasurementEntity, int>(data, filter, e => e.Id);

        float sum = ev.EvaluateSum(null, e => e.Weight);

        sum.Should().BeApproximately(4.0f, 0.001f);
    }

    // ── EvaluateSum — double selector ─────────────────────────────────────────

    [Fact]
    public void EvaluateSum_DoubleSelector_ReturnsCorrectSum()
    {
        var data = new List<MeasurementEntity>
        {
            new() { Id = 1, Score = 9.5,  IsActive = true },
            new() { Id = 2, Score = 8.75, IsActive = true },
            new() { Id = 3, Score = 7.0,  IsActive = true },
        };
        var ev = new ValiFlowEvaluator<MeasurementEntity, int>(data, getId: e => e.Id);

        double sum = ev.EvaluateSum(null, e => e.Score);

        sum.Should().BeApproximately(25.25, 0.0001);
    }

    // ── EvaluateDuplicatesByGroup — all unique → empty ────────────────────────

    [Fact]
    public void EvaluateDuplicatesByGroup_AllUnique_ReturnsEmptyDictionary()
    {
        // Each product in its own unique category → no group has > 1 item
        var data = new List<TestProduct>
        {
            new() { Id = 1, Category = "A", IsActive = true },
            new() { Id = 2, Category = "B", IsActive = true },
            new() { Id = 3, Category = "C", IsActive = true },
        };
        var ev = MakeEvaluator(data);

        var result = ev.EvaluateDuplicatesByGroup(null, p => p.Category);

        result.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateDuplicatesByGroup_SomeDuplicates_ReturnsOnlyDuplicatedGroups()
    {
        var data = MakeProducts(); // Electronics × 2, Books × 2 → both are duplicates
        var ev = MakeEvaluator(data);

        var result = ev.EvaluateDuplicatesByGroup(null, p => p.Category);

        result.Should().HaveCount(2);
        result["Electronics"].Should().HaveCount(2);
        result["Books"].Should().HaveCount(2);
    }

    // ── EvaluateUniquesByGroup — negateCondition = true ───────────────────────

    [Fact]
    public void EvaluateUniquesByGroup_NegateCondition_FiltersBeforeGrouping()
    {
        // Filter: IsActive=true → 2 items (Electronics + Books, each 1 item)
        // With negateCondition=true → keep IsActive=false → 2 items (same structure)
        var data = MakeProducts();
        var filter = new ValiFlow<TestProduct>().Add(p => p.IsActive);
        var ev = MakeEvaluator(data, filter);

        // negateCondition=true → only inactive (Id=2 Electronics, Id=4 Books)
        // each category has exactly 1 inactive → both are "unique"
        var result = ev.EvaluateUniquesByGroup(null, p => p.Category, negateCondition: true);

        result.Should().HaveCount(2);
        result["Electronics"].Id.Should().Be(2);
        result["Books"].Id.Should().Be(4);
    }

    [Fact]
    public void EvaluateUniquesByGroup_NegateCondition_GroupWithMultiple_ExcludesThat()
    {
        // Three items: 2 inactive Electronics, 1 inactive Books
        var data = new List<TestProduct>
        {
            new() { Id = 1, Category = "Electronics", IsActive = false },
            new() { Id = 2, Category = "Electronics", IsActive = false },
            new() { Id = 3, Category = "Books",       IsActive = false },
        };
        var filter = new ValiFlow<TestProduct>().Add(p => p.IsActive);
        var ev = MakeEvaluator(data, filter);

        // negateCondition=true → inactive items only
        // Electronics has 2 → NOT unique; Books has 1 → unique
        var result = ev.EvaluateUniquesByGroup(null, p => p.Category, negateCondition: true);

        result.Should().HaveCount(1);
        result["Books"].Id.Should().Be(3);
    }
}
