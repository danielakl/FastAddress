using System.Threading.Channels;

using FastAddress.Api.Services.Messages.Events;

using Microsoft.Extensions.Logging;

namespace FastAddress.Api.Services.Messages;

/// <summary>
/// Publishes events onto a bounded in-process channel. Non-blocking: a full channel drops the event
/// and logs it rather than back-pressuring the request thread.
/// </summary>
internal sealed partial class ChannelDomainEventPublisher(
    Channel<IDomainEvent> channel,
    ILogger<ChannelDomainEventPublisher> logger) : IDomainEventPublisher
{
    /// <inheritdoc/>
    public bool TryPublish(IDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (channel.Writer.TryWrite(@event))
        {
            return true;
        }

        LogEventDropped(logger, @event.EventType);
        return false;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Event channel full; dropped {EventType}")]
    private static partial void LogEventDropped(ILogger logger, string eventType);
}
