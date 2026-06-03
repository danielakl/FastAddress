using FastAddress.Api.Database.Models;
using FastAddress.Api.Services.Messages.Events;
using FastAddress.Api.Services.Models;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.TestUtilities;

using NetTopologySuite.Geometries;

namespace FastAddress.Api.Tests.Messages;

/// <summary>
/// Shared builders for the event-system tests. Every meaningful field is a parameter with a default,
/// so a test that asserts on a value passes it in and stays legible on its own.
/// </summary>
internal static class EventTestBuilders
{
    public static SearchStreetAddressQuery Query(
        string text = "Lade alle 77",
        int limit = 5,
        Point? locationBias = null) =>
        new() { Text = text, Limit = limit, LocationBias = locationBias };

    public static AddressSearchResult GoogleResult(
        string placeId,
        string type = "street_address",
        string streetName = "Lade alle",
        string streetNumber = "77") =>
        new()
        {
            PlaceId = placeId,
            OrderScore = 0,
            ShortFormattedAddress = $"{streetName} {streetNumber}",
            Location = GeoTestData.Point(10.0, 63.0),
            Types = [type],
            AddressComponents =
            [
                new AddressComponent { LongText = streetName, ShortText = streetName, Types = [AddressComponentTypes.Route] },
                new AddressComponent { LongText = streetNumber, ShortText = streetNumber, Types = [AddressComponentTypes.StreetNumber] },
            ],
        };

    public static StreetAddressUpsert Upsert(
        string placeId = "place-x",
        string streetLine = "Lade allé 77",
        string? postalCode = null,
        string? postalTown = null,
        string? country = null) =>
        new()
        {
            GooglePlaceId = placeId,
            StreetLine = streetLine,
            PostalCode = postalCode,
            PostalTown = postalTown,
            Country = country,
            SearchText = streetLine.ToUpperInvariant(),
            Location = GeoTestData.Point(10.0, 63.0),
        };

    public static GoogleResultsRetrieved Retrieved(
        string normalizedQuery = "LADE ALLE 77",
        params StreetAddressUpsert[] results) =>
        new(normalizedQuery, results);
}
