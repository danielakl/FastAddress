using FastAddress.Api.Controllers;
using FastAddress.Api.Services;
using FastAddress.Api.Services.Models;
using FastAddress.Sdk.Dto;
using FastAddress.TestUtilities;

using FluentValidation;

using NSubstitute;

namespace FastAddress.Api.Tests.Controllers;

public sealed class AddressControllerTests
{
    private readonly IAddressSearchService _searchService = Substitute.For<IAddressSearchService>();

    private static StreetAddressSearchEntry Entry(string streetLine) =>
        new()
        {
            PlaceId = $"place-{streetLine}",
            StreetLine = streetLine,
            Location = GeoTestData.Point(10.0, 63.0),
            Score = 1d,
        };

    private static SearchStreetAddressDto SearchDto(string? address = "Lade alle 77", int? limit = 5) =>
        new() { Address = address, Limit = limit, LocationBias = GeoTestData.Point(10.0, 63.0) };

    private void GivenSearchReturns(params StreetAddressSearchEntry[] entries) =>
        _searchService.Search(Arg.Any<SearchStreetAddressQuery>(), Arg.Any<CancellationToken>())
            .Returns(entries.AsAsyncEnumerable());

    [Fact]
    public async Task SearchAddresses_ValidRequest_ReturnsMappedStreetAddressDtos()
    {
        // Arrange
        GivenSearchReturns(Entry("Addr 0"), Entry("Addr 1"));
        var controller = new AddressController();

        // Act
        var results = await controller.SearchAddresses(SearchDto(), _searchService).CollectAsync();

        // Assert
        Assert.Equal(new[] { "Addr 0", "Addr 1" }, results.Select(r => r.StreetAddress));
    }

    [Fact]
    public async Task SearchAddresses_InvalidRequest_ThrowsValidationException()
    {
        // Arrange
        var controller = new AddressController();

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(async () =>
            await controller.SearchAddresses(SearchDto(address: ""), _searchService).CollectAsync());
    }

    [Fact]
    public async Task SearchAddresses_ValidRequest_PassesCleanedQueryToService()
    {
        // Arrange
        GivenSearchReturns();
        var controller = new AddressController();
        var searchDto = SearchDto(address: "  Lade alle 77  ", limit: 5);

        // Act
        await controller.SearchAddresses(searchDto, _searchService).CollectAsync();

        // Assert
        _searchService.Received(1).Search(
            Arg.Is<SearchStreetAddressQuery>(q => q.Text == "Lade alle 77" && q.Limit == 5 && q.LocationBias != null),
            Arg.Any<CancellationToken>());
    }
}
