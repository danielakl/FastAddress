using FastAddress.Api.Services.Messages.Events;

namespace FastAddress.Api.Services.Messages;

/// <summary>
/// Publishes domain events onto the local event bus without blocking the caller.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Try to enqueue an event for asynchronous handling.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the event was enqueued; <see langword="false"/> when the bus is
    /// full and the event was dropped (best-effort; the drop is logged).
    /// </returns>
    bool TryPublish(IDomainEvent @event);
}
