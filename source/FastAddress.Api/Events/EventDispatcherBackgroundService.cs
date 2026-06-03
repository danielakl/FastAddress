using System.Threading.Channels;

namespace FastAddress.Api.Events;

/// <summary>
/// Drains the domain-event channel on the application lifetime and dispatches each event to its
/// handler in a fresh DI scope. Errors are logged per event so one bad event never stops the loop.
/// </summary>
internal sealed partial class EventDispatcherBackgroundService(
    Channel<IDomainEvent> channel,
    IServiceScopeFactory scopes,
    ILogger<EventDispatcherBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var dispatch = scope.ServiceProvider
                    .GetRequiredKeyedService<IDomainEventDispatch>(@event.GetType());
                await dispatch.DispatchAsync(@event, scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                // Best-effort cache warming: swallow-and-log so the dispatcher survives a bad event.
                LogHandleFailed(logger, ex, @event.EventType);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to handle {EventType}")]
    private static partial void LogHandleFailed(ILogger logger, Exception exception, string eventType);
}
