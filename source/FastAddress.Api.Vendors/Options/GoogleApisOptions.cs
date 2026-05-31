namespace FastAddress.Api.Vendors.Options;

/// <summary>Options for Google APIs setup.</summary>
public sealed record GoogleApisOptions
{
    /// <summary>Expected key to find in the configuration store.</summary>
    public const string ConfigKey = "Google";

    /// <summary>API key used to access Google's APIs.</summary>
    /// <remarks>This is very sensitive information.</remarks>
    public required string ApiKey { get; init; }
}
