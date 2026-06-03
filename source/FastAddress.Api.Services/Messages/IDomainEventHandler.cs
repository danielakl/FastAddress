using FastAddress.Api.Services.Messages.Events;

namespace FastAddress.Api.Services.Messages;

/// <summary>
/// Handles a single concrete domain event type.
/// </summary>
/// <typeparam name="TEvent">The concrete event handled.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>Handle the event.</summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="ct">Token to cancel handling.</param>
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
