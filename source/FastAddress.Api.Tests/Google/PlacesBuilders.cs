using FastAddress.Api.Vendors.Google.Places.Models;

namespace FastAddress.Api.Tests.Google;

/// <summary>
/// Builders for Google Places API model graphs used across the places tests.
/// </summary>
internal static class PlacesBuilders
{
    public static PlacePrediction Prediction(string placeId) =>
        new() { PlaceId = placeId };

    public static AutocompleteResponse Autocomplete(params PlacePrediction?[] predictions) =>
        new() { Suggestions = predictions.Select(p => new Suggestion { PlacePrediction = p }).ToList() };

    public static PlaceDetailsResponse Place(
        string id,
        string? shortAddress = "Lade alle 77, Trondheim",
        double? longitude = 10.0,
        double? latitude = 63.0,
        string? movedPlaceId = null) =>
        new()
        {
            Id = id,
            MovedPlaceId = movedPlaceId,
            ShortFormattedAddress = shortAddress,
            Location = longitude is null || latitude is null
                ? null
                : new LatLng { Latitude = latitude.Value, Longitude = longitude.Value },
            Types = ["street_address"],
            AddressComponents = [],
        };
}
