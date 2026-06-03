namespace FastAddress.Api.Services.Messages.Events;

/// <summary>
/// Marker for a local domain event carried through the in-process event bus.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Stable discriminator.</summary>
    /// <example>google-places.result-retrieved</example>
    string EventType { get; }
}
