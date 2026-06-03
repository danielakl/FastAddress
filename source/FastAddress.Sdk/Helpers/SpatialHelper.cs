using System.Diagnostics.CodeAnalysis;

using NetTopologySuite.Geometries;

namespace FastAddress.Sdk.Helpers;

public static class SpatialHelper
{
    /// <summary>
    /// The World Geodetic System (WGS) is a standard for use in cartography, geodesy, and satellite navigation
    /// including GPS. This standard includes the definition of the coordinate system's fundamental and derived constants,
    /// the normal gravity Earth Gravitational Model (EGM), a description of the associated World Magnetic Model (WMM),
    /// and a current list of local datum transformations.<br/><br/>
    ///
    /// The latest revision is WGS 84 (also known as WGS 1984, EPSG:4326), established and maintained by the
    /// United States National Geospatial-Intelligence Agency since 1984, and last revised in 2014.<br/>
    /// <a href="https://en.wikipedia.org/wiki/World_Geodetic_System">World Geodetic System</a>
    /// </summary>
    public const int Srid = 4326;

    /// <summary>
    /// The precision model of the <see cref="Coordinate"/>s in a <see cref="Geometry"/> used. Model is using a
    /// <see cref="PrecisionModels.Fixed"/> number of decimal places of 5. This gives the model precision of down to
    /// ~1m.
    /// </summary>
    public static readonly PrecisionModel PrecisionModelInstance = new(scale: 100000d);

    /// <summary>
    /// Geometry factory instance using SRID 4326 and a precision model for coordinate precision down to ~1m.
    /// </summary>
    public static readonly GeometryFactory GeometryFactoryInstance = new(PrecisionModelInstance, srid: Srid);

    /// <summary>
    /// Produce a new <see cref="Point"/> rounded to the shared <see cref="PrecisionModelInstance"/>.
    /// The input is not mutated.
    /// </summary>
    /// <param name="point">The point to round.</param>
    /// <returns>A new precise point.</returns>
    [return: NotNullIfNotNull(nameof(point))]
    public static Point? MakePrecise(Point? point)
    {
        if (point is null || point.IsEmpty)
        {
            return point;
        }

        var coordinate = point.Coordinate.Copy();
        PrecisionModelInstance.MakePrecise(coordinate);
        return GeometryFactoryInstance.CreatePoint(coordinate);
    }

    /// <summary>
    /// Determine whether a point is non-null, non-empty and has finite ordinate values
    /// (via <see cref="Coordinate.IsValid"/>). Note: this does not enforce WGS84 range bounds.
    /// </summary>
    /// <param name="point">The point to validate.</param>
    /// <returns><see langword="true"/> when the point is usable as a spatial input.</returns>
    public static bool IsValidPoint([NotNullWhen(true)] Point? point)
    {
        return point is not null && !point.IsEmpty && point.Coordinate.IsValid;
    }
}
