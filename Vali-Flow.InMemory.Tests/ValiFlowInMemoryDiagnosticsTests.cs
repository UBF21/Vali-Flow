using System.Diagnostics;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Vali_Flow.InMemory.Tests.Models;

namespace Vali_Flow.InMemory.Tests;

public sealed class ValiFlowInMemoryDiagnosticsTests
{
    private static readonly List<TestProduct> Seed = new()
    {
        new() { Id = 1, Name = "Apple", Category = "Fruit", Price = 1.5m, Stock = 100, IsActive = true },
        new() { Id = 2, Name = "Donut", Category = "Sweet", Price = 2.5m, Stock = 50, IsActive = false },
    };

    private static ValiFlowEvaluator<TestProduct, int> CreateEvaluator()
        => new(Seed, null, p => p.Id);

    /// <summary>
    /// Attaches a listener and parents every captured activity under a private root, so concurrently
    /// running tests (xunit parallelizes test classes by default) can never pollute this test's capture
    /// list even though they share the same process-wide <see cref="ValiFlowDiagnostics.Source"/>.
    /// </summary>
    private static (ActivityListener Listener, Activity Root, List<Activity> Captured) AttachScopedListener()
    {
        var captured = new List<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ValiFlowDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = captured.Add
        };
        ActivitySource.AddActivityListener(listener);

        var root = new Activity("TestRoot").Start(); // becomes Activity.Current -> parents child activities
        return (listener, root, captured);
    }

    private static Activity GetOwn(List<Activity> captured, Activity root, string operationName)
        => captured.Single(a => a.OperationName == operationName && a.ParentId == root.Id);

    [Fact]
    public void EvaluateAny_WithTag_SetsTagAndEntityType()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;
        var evaluator = CreateEvaluator();
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.IsActive);

        evaluator.EvaluateAny(valiFlow: filter, tag: "MyReport");

        var activity = GetOwn(captured, root, "Vali-Flow.InMemory.EvaluateAny");
        activity.GetTagItem("vali_flow.tag").Should().Be("MyReport");
        activity.GetTagItem("vali_flow.entity_type").Should().Be(nameof(TestProduct));
    }

    [Fact]
    public void EvaluateCount_WithoutTag_SetsNoTag()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;
        var evaluator = CreateEvaluator();

        evaluator.EvaluateCount();

        var activity = GetOwn(captured, root, "Vali-Flow.InMemory.EvaluateCount");
        activity.GetTagItem("vali_flow.tag").Should().BeNull();
    }

    [Fact]
    public void EvaluateMin_NoMatchingEntities_RecordsErrorStatusAndRethrows()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;
        var evaluator = CreateEvaluator();
        // Filter matches nothing -> Min() on an empty int sequence throws InvalidOperationException.
        var filter = new ValiFlow<TestProduct>().IsTrue(p => p.Id == -1);

        Action act = () => evaluator.EvaluateMin(null, p => p.Stock, filter, tag: "Ingest");

        act.Should().Throw<InvalidOperationException>();
        var activity = GetOwn(captured, root, "Vali-Flow.InMemory.EvaluateMin");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem("vali_flow.tag").Should().Be("Ingest");
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }

    [Fact]
    public void EvaluateGrouped_WithTag_SetsTagAndEntityType()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;
        var evaluator = CreateEvaluator();

        evaluator.EvaluateGrouped(null, p => p.Category, tag: "GroupReport");

        var activity = GetOwn(captured, root, "Vali-Flow.InMemory.EvaluateGrouped");
        activity.GetTagItem("vali_flow.tag").Should().Be("GroupReport");
        activity.GetTagItem("vali_flow.entity_type").Should().Be(nameof(TestProduct));
    }

    [Fact]
    public void EvaluateTopByGroup_InvalidCount_RecordsErrorStatus()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;
        var evaluator = CreateEvaluator();

        Action act = () => evaluator.EvaluateTopByGroup<string, int>(null, p => p.Category, count: 0, tag: "BadCount");

        act.Should().Throw<ArgumentOutOfRangeException>();
        var activity = GetOwn(captured, root, "Vali-Flow.InMemory.EvaluateTopByGroup");
        activity.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
    public void EvaluatePagedResult_InvalidPage_RecordsErrorStatus()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;
        var evaluator = CreateEvaluator();

        Action act = () => evaluator.EvaluatePagedResult<int>(page: 0, tag: "BadPage");

        act.Should().Throw<ArgumentOutOfRangeException>();
        var activity = GetOwn(captured, root, "Vali-Flow.InMemory.EvaluatePagedResult");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem("vali_flow.tag").Should().Be("BadPage");
    }
}
