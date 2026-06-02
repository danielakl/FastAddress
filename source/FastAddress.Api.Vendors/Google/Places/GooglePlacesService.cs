using System.Runtime.CompilerServices;

using FastAddress.Api.Vendors.Contracts;
using FastAddress.Api.Vendors.Google.Places.Models;
using FastAddress.Api.Vendors.Models;
using FastAddress.Sdk.Helpers;

using NetTopologySuite.Geometries;

namespace FastAddress.Api.Vendors.Google.Places;

/// <summary>
/// <see cref="IGooglePlacesService"/> implementation.
/// </summary>
internal sealed class GooglePlacesService(IPlacesApi places) : IGooglePlacesService
{
    private const string LanguageCode = "en-US";
    private const string RegionCode = "no";
    private const double LocationBiasRadiusMeters = 25_000;
    private const string PlaceAutoCompleteFields = "suggestions.placePrediction.placeId,suggestions.placePrediction.types,suggestions.placePrediction.text.text";
    private const string PlaceDetailsFields = "id,movedPlaceId,addressComponents,shortFormattedAddress,location,types";

    /// <inheritdoc/>
    public async IAsyncEnumerable<AddressSearchResult> Search(
        AddressSearchRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            yield break;
        }

        var autocomplete = await places.AutocompleteAsync(
            new AutocompleteRequest
            {
                IncludedPrimaryTypes = [..PlaceTypes.StreetAddressTypes],
                IncludedRegionCodes = [RegionCode],
                Input = request.Query,
                LocationBias = ToLocationBias(request.LocationBias),
            },
            PlaceAutoCompleteFields,
            LanguageCode,
            RegionCode,
            ct);

        if (autocomplete.Suggestions is null)
        {
            yield break;
        }

        var limit = request.Limit ?? autocomplete.Suggestions.Count;
        var predictions = autocomplete.Suggestions
            .Select(s => s.PlacePrediction)
            .Where(p => p is not null)
            .Take(limit)
            .ToList();

        if (predictions.Count == 0)
        {
            yield break;
        }

        var pending = new List<Task<AddressSearchResult?>>(predictions.Select((t, i) => FetchAsync(t!.PlaceId, orderScore: i, ct)));

        while (pending.Count > 0)
        {
            var completed = await Task.WhenAny(pending);
            pending.Remove(completed);

            var result = await completed;
            if (result is not null)
            {
                yield return result;
            }
        }
    }

    /// <summary>Wrap a bias point in a fixed-radius circle; returns <see langword="null"/> when absent so
    /// Google fallsback to its default IP-based bias.</summary>
    private static LocationBias? ToLocationBias(Point? point) =>
        point is null
            ? null
            : new LocationBias
            {
                Circle = new Circle
                {
                    Center = new LatLng { Latitude = point.Y, Longitude = point.X },
                    Radius = LocationBiasRadiusMeters,
                },
            };

    private async Task<AddressSearchResult?> FetchAsync(string placeId, int orderScore, CancellationToken ct)
    {
        var details = await places.GetPlaceAsync(
            placeId,
            PlaceDetailsFields,
            LanguageCode,
            RegionCode,
            ct);

        if (details.Location is null || details.ShortFormattedAddress is null)
        {
            return null;
        }

        var point = SpatialHelper.GeometryFactoryInstance.CreatePoint(
            new Coordinate(details.Location.Longitude, details.Location.Latitude));

        return new AddressSearchResult
        {
            AddressComponents = details.AddressComponents ?? [],
            Location = point,
            OrderScore = orderScore,
            PlaceId = details.MovedPlaceId ?? details.Id,
            ShortFormattedAddress = details.ShortFormattedAddress,
            Types = details.Types ?? [],
        };
    }
}
