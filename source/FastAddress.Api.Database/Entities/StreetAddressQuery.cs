// ReSharper disable always EntityFramework.ModelValidation.UnlimitedStringLength - Configured with fluent syntax in DB context.

using NodaTime;

namespace FastAddress.Api.Database.Entities;

/// <summary>
/// Ledger of the normalized queries we have asked Google about. Each query signal its freshness through
/// <see cref="LastRefreshed"/>. While it's fresh the request path serves
/// <see cref="StreetAddressResult"/> rows from the database without consulting Google.
/// </summary>
public sealed class StreetAddressQuery : IEntityTimestamps
{
    /// <summary>Identifier.</summary>
    public long Id { get; set; }

    /// <inheritdoc/>
    public Instant Added { get; set; }

    /// <inheritdoc/>
    public Instant Modified { get; set; }

    /// <summary>Normalized query text (upper-cased, single-line). Unique per query.</summary>
    /// <example>LADE ALLÉ 77</example>
    public required string Query { get; set; }

    /// <summary>Timestamp of the last successful Google fetch for this query..</summary>
    public Instant LastRefreshed { get; set; }
}
