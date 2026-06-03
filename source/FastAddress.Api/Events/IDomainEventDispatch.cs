namespace FastAddress.Api.Events;

/// <summary>
/// Non-generic bridge that lets the dispatcher invoke a strongly-typed
/// <see cref="IDomainEventHandler{TEvent}"/>. One closed implementation is
/// registered per event type, keyed on the runtime event <see cref="Type"/>.
/// </summary>
internal interface IDomainEventDispatch
{
    /// <summary>Resolve the handler from <paramref name="scope"/> and dispatch the event to it.</summary>
    Task DispatchAsync(IDomainEvent @event, IServiceProvider scope, CancellationToken ct);
}
