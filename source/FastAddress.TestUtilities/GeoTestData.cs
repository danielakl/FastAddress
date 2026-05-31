using FastAddress.Sdk.Helpers;

using NetTopologySuite.Geometries;

namespace FastAddress.TestUtilities;

/// <summary>
/// Helpers for building geographic test data using the production <see cref="SpatialHelper"/> factory,
/// so test points share the same SRID and precision model as runtime values.
/// </summary>
public static class GeoTestData
{
    /// <summary>
    /// Create a <see cref="Point"/> using the shared WGS84 geometry factory.
    /// </summary>
    /// <param name="longitude">Longitude in degrees.</param>
    /// <param name="latitude">Latitude in degrees.</param>
    /// <returns>A point with SRID <see cref="SpatialHelper.Srid"/>.</returns>
    public static Point Point(double longitude, double latitude) =>
        SpatialHelper.GeometryFactoryInstance.CreatePoint(new Coordinate(longitude, latitude));
}
