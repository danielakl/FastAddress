using FastAddress.Api.Services.Messages.Events;

using Microsoft.Extensions.DependencyInjection;

namespace FastAddress.Api.Services.Messages;

/// <summary>
/// Closed-generic dispatch bridge for <typeparamref name="TEvent"/>. The single <c>(TEvent)</c> cast
/// here is the only place the runtime event is narrowed to its concrete type.
/// </summary>
/// <typeparam name="TEvent">The concrete event this bridge dispatches.</typeparam>
internal sealed class DomainEventDispatch<TEvent> : IDomainEventDispatch
    where TEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Task DispatchAsync(IDomainEvent @event, IServiceProvider scope, CancellationToken ct) =>
        scope.GetRequiredService<IDomainEventHandler<TEvent>>().HandleAsync((TEvent)@event, ct);
}
