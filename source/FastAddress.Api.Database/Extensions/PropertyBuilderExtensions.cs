using FastAddress.Sdk.Helpers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NetTopologySuite.Geometries;

using NodaTime;

namespace FastAddress.Api.Database.Extensions;

/// <summary>
/// Extensions for <see cref="PropertyBuilder{TProperty}"/>.
/// </summary>
public static class PropertyBuilderExtensions
{
    // Returns a "timestamp with time zone". now() -> 2019-12-23 14:39:53.662522-05
    private const string PostgreSqlNow = "now()";

    /// <summary>
    /// Configures the timestamp property to default to now() on insert if the value is its BCL default.
    /// </summary>
    /// <param name="builder">The property builder.</param>
    public static PropertyBuilder<Instant> HasDefaultValueNow(this PropertyBuilder<Instant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasDefaultValueSql(PostgreSqlNow);
    }

    /// <summary>
    /// Maps a <see cref="Point"/> property to the PostGIS <c>geography</c> type at <see cref="SpatialHelper.Srid"/>.
    /// </summary>
    /// <remarks>
    /// The default NetTopologySuite mapping is PostGIS <c>geometry</c>, whose <c>ST_Distance</c> is planar
    /// and returns degrees for SRID 4326. <c>geography</c> is geodetic, so distances come back in metres —
    /// which the proximity bias requires (§4.5). The column stays GiST-indexable.
    /// </remarks>
    /// <param name="builder">The property builder.</param>
    public static PropertyBuilder<Point?> HasGeographyColumnType(this PropertyBuilder<Point?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasColumnType($"geography(Point,{SpatialHelper.Srid})");
    }
}
