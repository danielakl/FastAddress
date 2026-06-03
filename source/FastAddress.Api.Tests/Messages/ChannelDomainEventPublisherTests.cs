using System.Threading.Channels;

using FastAddress.Api.Services.Messages.Events;

using Microsoft.Extensions.Logging.Abstractions;

using ChannelDomainEventPublisher = FastAddress.Api.Services.Messages.ChannelDomainEventPublisher;

namespace FastAddress.Api.Tests.Messages;

public sealed class ChannelDomainEventPublisherTests
{
    private static Channel<IDomainEvent> BoundedChannel(int capacity) =>
        Channel.CreateBounded<IDomainEvent>(
            new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait });

    private static ChannelDomainEventPublisher PublisherOver(Channel<IDomainEvent> channel) =>
        new(channel, NullLogger<ChannelDomainEventPublisher>.Instance);

    [Fact]
    public void TryPublish_ChannelHasCapacity_EnqueuesAndReturnsTrue()
    {
        // Arrange
        var channel = BoundedChannel(capacity: 1);
        var publisher = PublisherOver(channel);
        var @event = new AddressSearchPerformed(EventTestBuilders.Query(), MatchCount: 0);

        // Act
        var published = publisher.TryPublish(@event);

        // Assert
        Assert.True(published);
        Assert.True(channel.Reader.TryRead(out var written));
        Assert.Same(@event, written);
    }

    [Fact]
    public void TryPublish_ChannelFull_DropsAndReturnsFalse()
    {
        // Arrange — fill the only slot so the next write has nowhere to go.
        var channel = BoundedChannel(capacity: 1);
        var publisher = PublisherOver(channel);
        publisher.TryPublish(new AddressSearchPerformed(EventTestBuilders.Query(), MatchCount: 0));

        // Act
        var published = publisher.TryPublish(new AddressSearchPerformed(EventTestBuilders.Query(), MatchCount: 0));

        // Assert
        Assert.False(published);
    }
}
