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

    [Fact]
    public void MakePrecise_HighPrecisionPoint_RoundsToFiveDecimalsAndTagsSrid()
    {
        // Arrange
        var raw = new Point(new Coordinate(10.123456789, 63.987654321)) { SRID = SpatialHelper.Srid };

        // Act
        var precise = SpatialHelper.MakePrecise(raw);

        // Assert
        Assert.Equal(10.12346, precise!.X, precision: 5);
        Assert.Equal(63.98765, precise.Y, precision: 5);
        Assert.Equal(SpatialHelper.Srid, precise.SRID);
    }

    [Fact]
    public void MakePrecise_DoesNotMutateInput()
    {
        // Arrange
        var raw = new Point(new Coordinate(10.123456789, 63.987654321)) { SRID = SpatialHelper.Srid };

        // Act
        SpatialHelper.MakePrecise(raw);

        // Assert — the original keeps its full precision.
        Assert.Equal(10.123456789, raw.X);
    }

    [Fact]
    public void MakePrecise_Null_ReturnsNull()
    {
        // Act + Assert
        Assert.Null(SpatialHelper.MakePrecise(null));
    }

    [Fact]
    public void IsValidPoint_FinitePoint_ReturnsTrue()
    {
        // Arrange
        var point = new Point(new Coordinate(10.0, 63.0));

        // Act + Assert
        Assert.True(SpatialHelper.IsValidPoint(point));
    }

    [Theory]
    [InlineData(double.NaN, 63.0)]
    [InlineData(10.0, double.PositiveInfinity)]
    public void IsValidPoint_NonFiniteOrdinate_ReturnsFalse(double x, double y)
    {
        // Arrange
        var point = new Point(new Coordinate(x, y));

        // Act + Assert
        Assert.False(SpatialHelper.IsValidPoint(point));
    }

    [Fact]
    public void IsValidPoint_Null_ReturnsFalse()
    {
        // Act + Assert
        Assert.False(SpatialHelper.IsValidPoint(null));
    }

    [Fact]
    public void IsValidPoint_EmptyPoint_ReturnsFalse()
    {
        // Act + Assert
        Assert.False(SpatialHelper.IsValidPoint(SpatialHelper.GeometryFactoryInstance.CreatePoint()));
    }
}
