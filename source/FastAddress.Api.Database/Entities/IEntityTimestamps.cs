using NodaTime;

namespace FastAddress.Api.Database.Entities;

/// <summary>
/// Timestamps for DB entities tracking when the entity was created or last updated.
/// </summary>
public interface IEntityTimestamps
{
    /// <summary>
    /// UTC timestamp when this entity was created.
    /// </summary>
    Instant Added { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was last modified.
    /// </summary>
    public Instant Modified { get; set; }
}
