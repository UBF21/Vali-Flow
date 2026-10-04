using System.Diagnostics;
using Vali_Flow.Abstractions.Diagnostics;

namespace Vali_Flow.Abstractions.Tests.Diagnostics;

public sealed class ValiFlowDiagnosticsTests
{
    private static ActivityListener AttachListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ValiFlowDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public void StartActivity_NoListenerAttached_ReturnsNull()
    {
        // Without a listener, ActivitySource.StartActivity short-circuits to null — near-zero cost.
        Activity? activity = ValiFlowDiagnostics.StartActivity("Test.Operation");

        activity.Should().BeNull();
    }

    [Fact]
    public void StartActivity_WithListener_SetsTagAndEntityType()
    {
        using var listener = AttachListener();

        using Activity? activity = ValiFlowDiagnostics.StartActivity("Test.Operation", tag: "MyReport", entityType: "User");

        activity.Should().NotBeNull();
        activity!.GetTagItem("vali_flow.tag").Should().Be("MyReport");
        activity.GetTagItem("vali_flow.entity_type").Should().Be("User");
    }

    [Fact]
    public void StartActivity_WithoutTagOrEntityType_SetsNoExtraTags()
    {
        using var listener = AttachListener();

        using Activity? activity = ValiFlowDiagnostics.StartActivity("Test.Operation");

        activity.Should().NotBeNull();
        activity!.GetTagItem("vali_flow.tag").Should().BeNull();
        activity.GetTagItem("vali_flow.entity_type").Should().BeNull();
    }

    [Fact]
    public void RecordException_NullActivity_DoesNothing()
    {
        var act = () => ValiFlowDiagnostics.RecordException(null, new InvalidOperationException("boom"));

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_SetsErrorStatusAndExceptionEvent()
    {
        using var listener = AttachListener();
        using Activity? activity = ValiFlowDiagnostics.StartActivity("Test.Operation");

        var ex = new InvalidOperationException("boom");
        ValiFlowDiagnostics.RecordException(activity, ex);

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("boom");
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }
}
