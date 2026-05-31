using FastAddress.Sdk.Helpers;

using NetTopologySuite.Geometries;

namespace FastAddress.Sdk.Tests.Helpers;

public sealed class SpatialHelperTests
{
    [Fact]
    public void Srid_Value_IsWgs84()
    {
        // Assert
        Assert.Equal(4326, SpatialHelper.Srid);
    }

    [Fact]
    public void PrecisionModelInstance_Scale_GivesMetreLevelPrecision()
    {
        // Assert
        Assert.Equal(100000d, SpatialHelper.PrecisionModelInstance.Scale);
    }

    [Fact]
    public void GeometryFactoryInstance_CreatedPoint_UsesConfiguredSrid()
    {
        // Act
        var point = SpatialHelper.GeometryFactoryInstance.CreatePoint(new Coordinate(10.0, 63.0));

        // Assert
        Assert.Equal(SpatialHelper.Srid, point.SRID);
    }
}
