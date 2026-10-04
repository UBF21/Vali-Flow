using System.Diagnostics;

namespace Vali_Flow.Abstractions.Diagnostics;

/// <summary>
/// Shared <see cref="ActivitySource"/> for the whole Vali-Flow ecosystem. Every evaluator/translator
/// starts an <see cref="Activity"/> per operation through <see cref="StartActivity"/> — this is the
/// "tag"/observability hook across packages: no logging framework dependency, works out of the box
/// with any OpenTelemetry-compatible listener (Application Insights, Jaeger, Datadog, etc.), and does
/// nothing (near-zero cost) when no listener is attached.
/// </summary>
/// <remarks>
/// To observe these activities, register a listener once at application startup, e.g. via
/// OpenTelemetry's <c>AddSource("Vali-Flow")</c>, or a minimal <see cref="ActivityListener"/>.
/// </remarks>
public static class ValiFlowDiagnostics
{
    /// <summary>The <see cref="ActivitySource"/> name every Vali-Flow package shares. Use this with <c>AddSource(...)</c>.</summary>
    public const string SourceName = "Vali-Flow";

    /// <summary>The shared <see cref="ActivitySource"/> instance used by every Vali-Flow package.</summary>
    public static readonly ActivitySource Source = new(SourceName, "1.0.0");

    /// <summary>
    /// Starts an <see cref="Activity"/> for a Vali-Flow operation (query evaluation, write, translation).
    /// Returns <c>null</c> when no listener is attached — callers must null-check before using the result,
    /// same as any <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> call.
    /// </summary>
    /// <param name="operationName">The operation name, e.g. <c>"Vali-Flow.EvaluateQueryAsync"</c>.</param>
    /// <param name="tag">
    /// Optional developer-supplied label (the "TagWith"-style tag) identifying why/where this
    /// operation was triggered — set as the <c>vali_flow.tag</c> tag when present.
    /// </param>
    /// <param name="entityType">Optional entity type name — set as the <c>vali_flow.entity_type</c> tag.</param>
    public static Activity? StartActivity(string operationName, string? tag = null, string? entityType = null)
    {
        Activity? activity = Source.StartActivity(operationName, ActivityKind.Internal);
        if (activity == null) return null;

        if (tag != null) activity.SetTag("vali_flow.tag", tag);
        if (entityType != null) activity.SetTag("vali_flow.entity_type", entityType);

        return activity;
    }

    /// <summary>
    /// Records a failed operation on the given activity: sets an error status and attaches an
    /// OpenTelemetry-style <c>exception</c> event, then returns — the caller is still responsible
    /// for rethrowing. Never swallows the exception.
    /// </summary>
    public static void RecordException(Activity? activity, Exception ex)
    {
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            { "exception.type", ex.GetType().FullName },
            { "exception.message", ex.Message },
            { "exception.stacktrace", ex.StackTrace }
        }));
    }
}
