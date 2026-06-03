using FastAddress.Api.Services.Messages;
using FastAddress.Api.Services.Messages.Events;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace FastAddress.Api.Tests.Messages;

public sealed class DomainEventDispatchTests
{
    [Fact]
    public async Task DispatchAsync_ResolvesHandlerFromScopeAndForwardsTypedEvent()
    {
        // Arrange - the bridge must resolve IDomainEventHandler<GoogleResultsRetrieved> and hand it
        // the event narrowed to its concrete type.
        var handler = Substitute.For<IDomainEventHandler<GoogleResultsRetrieved>>();
        await using var scope = new ServiceCollection()
            .AddSingleton(handler)
            .BuildServiceProvider();
        var @event = EventTestBuilders.Retrieved();

        // Act
        await new DomainEventDispatch<GoogleResultsRetrieved>()
            .DispatchAsync(@event, scope, CancellationToken.None);

        // Assert
        await handler.Received(1).HandleAsync(@event, CancellationToken.None);
    }
}
