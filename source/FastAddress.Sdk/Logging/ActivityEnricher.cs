using System.Diagnostics;

using Serilog.Core;
using Serilog.Events;

namespace FastAddress.Sdk.Logging;

/// <summary>
/// Adds the current <see cref="Activity"/> correlation identifiers (TraceId, SpanId, ParentId) to
/// each log event so logs can be tied to a distributed trace. Reads <see cref="Activity.Current"/>
/// and supports both the W3C and hierarchical id formats. Does nothing when no activity is active.
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    /// <inheritdoc/>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(propertyFactory);

        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        AddIfPresent(logEvent, propertyFactory, "TraceId", GetTraceId(activity));
        AddIfPresent(logEvent, propertyFactory, "SpanId", GetSpanId(activity));
        AddIfPresent(logEvent, propertyFactory, "ParentId", GetParentId(activity));
    }

    private static void AddIfPresent(
        LogEvent logEvent,
        ILogEventPropertyFactory propertyFactory,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name, value));
        }
    }

    private static string? GetTraceId(Activity activity) => activity.IdFormat switch
    {
        ActivityIdFormat.Hierarchical => activity.RootId,
        ActivityIdFormat.W3C => activity.TraceId.ToHexString(),
        _ => null,
    };

    private static string? GetSpanId(Activity activity) => activity.IdFormat switch
    {
        ActivityIdFormat.Hierarchical => activity.Id,
        ActivityIdFormat.W3C => activity.SpanId.ToHexString(),
        _ => null,
    };

    private static string? GetParentId(Activity activity) => activity.IdFormat switch
    {
        ActivityIdFormat.Hierarchical => activity.ParentId,
        ActivityIdFormat.W3C => activity.ParentSpanId.ToHexString(),
        _ => null,
    };
}
