using FastAddress.Api.Controllers;
using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Dto;
using FastAddress.TestUtilities;

using FluentValidation;

using NSubstitute;

namespace FastAddress.Api.Tests.Controllers;

public sealed class AddressControllerTests
{
    private readonly IGooglePlacesService googlePlaces = Substitute.For<IGooglePlacesService>();

    private static AddressSearchResult SearchResult(int orderScore, string address) =>
        new()
        {
            PlaceId = $"place-{orderScore}",
            OrderScore = orderScore,
            ShortFormattedAddress = address,
            Location = GeoTestData.Point(10.0, 63.0),
            Types = ["street_address"],
            AddressComponents = Array.Empty<AddressComponent>(),
        };

    private void GivenSearchReturns(params AddressSearchResult[] results) =>
        googlePlaces.SearchAsync(Arg.Any<AddressSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(results.AsAsyncEnumerable());

    [Fact]
    public async Task SearchAddressesAsync_ValidRequest_ReturnsMappedAddressDtos()
    {
        // Arrange
        GivenSearchReturns(SearchResult(0, "Addr 0"), SearchResult(1, "Addr 1"));
        var controller = new AddressController();
        var searchDto = new SearchAddressDto { Center = null, Address = "Lade alle 77", Radius = 1000 };

        // Act
        var results = await controller.SearchAddressesAsync(searchDto, googlePlaces).CollectAsync();

        // Assert
        Assert.Equal(new[] { "Addr 0", "Addr 1" }, results.Select(r => r.StreetAddress));
    }

    [Fact]
    public async Task SearchAddressesAsync_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var controller = new AddressController();
        var searchDto = new SearchAddressDto { Center = null, Address = "", Radius = null };

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await controller.SearchAddressesAsync(searchDto, googlePlaces).CollectAsync());
    }

    [Fact]
    public async Task SearchAddressesAsync_ValidRequest_PassesCleanedQueryAndLimitToService()
    {
        // Arrange
        GivenSearchReturns();
        var controller = new AddressController();
        var searchDto = new SearchAddressDto { Center = null, Address = "  Lade alle 77  ", Radius = 1000, Limit = 5 };

        // Act
        await controller.SearchAddressesAsync(searchDto, googlePlaces).CollectAsync();

        // Assert
        googlePlaces.Received(1).SearchAsync(
            Arg.Is<AddressSearchRequest>(r => r.Query == "Lade alle 77" && r.Limit == 5),
            Arg.Any<CancellationToken>());
    }
}
