using System.Threading.Channels;

using FastAddress.Api.Events.Handlers;

namespace FastAddress.Api.Events.Extensions;

/// <summary>
/// DI registration for the local in-process domain-event bus.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const int ChannelCapacity = 10_000;

    /// <summary>
    /// Register the bounded channel, publisher, background dispatcher, and the event handlers.
    /// </summary>
    public static IServiceCollection AddDomainEventSystem(this IServiceCollection services)
    {
        // The single channel is the only transport registered. Publisher and dispatcher take its
        // Writer/Reader internally so nothing else can reach the transport.
        // FullMode.Wait (paired with the publisher's non-blocking TryWrite) reports a dropped write
        // as a false return, which DropWrite would silently swallow.
        services.AddSingleton(_ => Channel.CreateBounded<IDomainEvent>(
            new BoundedChannelOptions(ChannelCapacity) { FullMode = BoundedChannelFullMode.Wait }));
        services.AddSingleton<IDomainEventPublisher, ChannelDomainEventPublisher>();
        services.AddHostedService<EventDispatcherBackgroundService>();

        // One line per event type: scoped handler + keyed dispatch bridge resolved by runtime type.
        services.AddScoped<IDomainEventHandler<AddressSearchPerformed>, AddressSearchEventHandler>();
        services.AddKeyedSingleton<IDomainEventDispatch, DomainEventDispatch<AddressSearchPerformed>>(
            typeof(AddressSearchPerformed));

        return services;
    }
}
