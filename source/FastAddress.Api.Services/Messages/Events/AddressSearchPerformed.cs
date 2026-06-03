using FastAddress.Api.Services.Models;

namespace FastAddress.Api.Services.Messages.Events;

/// <summary>
/// Raised after a street-address search has been served from the database, carrying the original
/// query and how many matches the cache returned. The background handler uses the count to decide
/// whether to refresh the cache from Google.
/// </summary>
public sealed record AddressSearchPerformed(SearchStreetAddressQuery Query, int MatchCount) : IDomainEvent
{
    /// <inheritdoc/>
    public string EventType => "address.search-performed";
}
