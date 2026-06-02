namespace FastAddress.Api.Vendors.Google.Places.Models;

/// <summary>
/// Response from Google Places API - <c>places:autocomplete</c>.
/// </summary>
public sealed record AutocompleteResponse
{
    /// <summary>Suggestions list.</summary>
    public IReadOnlyList<Suggestion>? Suggestions { get; init; }
}

/// <summary>One autocomplete suggestion.</summary>
public sealed record Suggestion
{
    /// <summary>Place prediction payload. Always populated for place suggestions.</summary>
    public required PlacePrediction? PlacePrediction { get; init; }
}

/// <summary>Predicted place metadata returned from autocomplete.</summary>
public sealed record PlacePrediction
{
    /// <summary>Place identifier.</summary>
    /// <example>EhdMYWRlIGF</example>
    public required string PlaceId { get; init; }

    /// <summary>Display text for the prediction.</summary>
    public FormattableText? Text { get; init; }

    /// <summary>Place types.</summary>
    /// <example>street_address, route</example>
    public IReadOnlyList<string>? Types { get; init; }
}

/// <summary>Text with display ranges (highlighting).</summary>
public sealed record FormattableText
{
    /// <summary>Display text.</summary>
    public required string Text { get; init; }
}
