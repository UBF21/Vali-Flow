using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Abstractions.Diagnostics;
using Vali_Flow.Classes.Evaluators;
using Vali_Flow.Classes.Specification;
using Vali_Flow.Core.Builder;
using Vali_Flow.Tests.Infrastructure;
using Vali_Flow.Tests.Models;
using Xunit;

namespace Vali_Flow.Tests;

/// <summary>
/// Verifies that <see cref="ValiFlowEvaluator{T}"/> read/write operations open a
/// <see cref="ValiFlowDiagnostics"/> <see cref="Activity"/> with the expected name/tags,
/// and mark it as failed when the underlying operation throws.
/// </summary>
public sealed class ValiFlowEfDiagnosticsTests
{
    private static TestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

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
    public async Task EvaluateCountAsync_WithTagWith_StartsActivityWithExpectedNameAndTag()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        await using var ctx = CreateContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(new ValiFlowQuery<TestOrder>())
            .WithTagWith("MyReport");

        await evaluator.EvaluateCountAsync(spec);

        Activity activity = GetOwn(captured, root, "Vali-Flow.EvaluateCountAsync");
        activity.GetTagItem("vali_flow.tag").Should().Be("MyReport");
        activity.GetTagItem("vali_flow.entity_type").Should().Be(nameof(TestOrder));
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task EvaluateAnyAsync_SetsHasFilterAndIncludeCountTags()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        await using var ctx = CreateContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new BasicSpecification<TestOrder>(new ValiFlowQuery<TestOrder>().IsTrue(o => o.IsShipped));

        await evaluator.EvaluateAnyAsync(spec);

        Activity activity = GetOwn(captured, root, "Vali-Flow.EvaluateAnyAsync");
        activity.GetTagItem("vali_flow.has_filter").Should().Be(true);
        activity.GetTagItem("vali_flow.include_count").Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_NullEntity_RecordsActivityAsError()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        await using var ctx = CreateContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => evaluator.AddAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();

        Activity activity = GetOwn(captured, root, "Vali-Flow.AddAsync");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }

    [Fact]
    public async Task ExecuteTransactionAsync_OperationThrows_RecordsActivityAsError()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        await using var ctx = CreateContext();
        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);

        Func<Task> act = () => evaluator.ExecuteTransactionAsync(
            () => throw new InvalidOperationException("boom"));

        // The InMemory provider does not support transactions, so BeginTransactionAsync itself
        // throws — either way the operation fails and the activity must be recorded as errored.
        await act.Should().ThrowAsync<Exception>();

        Activity activity = GetOwn(captured, root, "Vali-Flow.ExecuteTransactionAsync");
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }

    [Fact]
    public async Task EvaluatePagedAsync_SetsPageAndPageSizeTags()
    {
        var (listener, root, captured) = AttachScopedListener();
        using var l = listener;
        using var r = root;

        await using var ctx = CreateContext();
        ctx.Orders.Add(new TestOrder { Id = 1, CustomerName = "Alice", Total = 10m, CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var evaluator = new ValiFlowEvaluator<TestOrder>(ctx);
        var spec = new QuerySpecification<TestOrder>(new ValiFlowQuery<TestOrder>())
            .WithPage(1)
            .WithPageSize(10)
            .WithOrderBy(o => o.Id);

        await evaluator.EvaluatePagedAsync(spec);

        Activity activity = GetOwn(captured, root, "Vali-Flow.EvaluatePagedAsync");
        activity.GetTagItem("vali_flow.page").Should().Be(1);
        activity.GetTagItem("vali_flow.page_size").Should().Be(10);
        activity.GetTagItem("vali_flow.has_pagination").Should().Be(true);
    }
}
