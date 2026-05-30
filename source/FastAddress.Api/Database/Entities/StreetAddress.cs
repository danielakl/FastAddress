using NodaTime;

namespace FastAddress.Api.Database.Entities;

public sealed class StreetAddress : IEntityTimestamps
{
    public long Id { get; set; }

    /// <inheritdoc/>
    public Instant Added { get; set; }

    /// <inheritdoc/>
    public Instant Modified { get; set; }
    
    public string GooglePlaceId { get; set; }
    public string Address { get; set; }
}
