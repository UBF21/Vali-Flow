using FluentAssertions;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;
using Xunit;

namespace Vali_Flow.InMemory.Tests;

public sealed class ValiFlowInMemoryUpsertRangeTests
{
    private static TestProduct P(int id, string name, decimal price = 10m) =>
        new() { Id = id, Name = name, Price = price, IsActive = true, Stock = 1, Category = "X" };

    // UpsertRange — duplicates in same batch

    [Fact]
    public void UpsertRange_DuplicateIdsInBatch_LastWins()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>();

        ev.UpsertRange(new[]
        {
            P(1, "First"),
            P(1, "Second"),  // same ID — should win
        });
        ev.SaveChanges();

        var result = ev.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1));
        result.Should().NotBeNull();
        result!.Name.Should().Be("Second");
    }

    [Fact]
    public void UpsertRange_NoDuplicates_AllInserted()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>();

        ev.UpsertRange(new[] { P(1, "A"), P(2, "B"), P(3, "C") });
        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(3);
    }

    [Fact]
    public void UpsertRange_ExistingEntities_Updates()
    {
        var initial = new List<TestProduct> { P(1, "Old", 10m) };
        var ev = new ValiFlowEvaluator<TestProduct, int>(initial);

        ev.UpsertRange(new[] { P(1, "New", 99m), P(2, "Added") });
        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(2);
        var updated = ev.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1));
        updated!.Name.Should().Be("New");
        updated.Price.Should().Be(99m);
    }

    [Fact]
    public void UpsertRange_WithNonListExternalStore_ThrowsArgumentException()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>();
        IEnumerable<TestProduct> array = new[] { P(1, "A") };

        Action act = () => ev.UpsertRange(new[] { P(1, "A") }, array);

        act.Should().Throw<ArgumentException>().WithParameterName("entities");
    }

    [Fact]
    public void UpsertRange_TripleDuplicate_LastOccurrenceWins()
    {
        var ev = new ValiFlowEvaluator<TestProduct, int>();

        ev.UpsertRange(new[]
        {
            P(1, "First",  10m),
            P(1, "Second", 20m),
            P(1, "Third",  30m),  // should win
        });
        ev.SaveChanges();

        ev.EvaluateCount(null).Should().Be(1);
        var result = ev.GetFirst(null, new ValiFlow<TestProduct>().EqualTo(p => p.Id, 1));
        result!.Name.Should().Be("Third");
        result.Price.Should().Be(30m);
    }
}
