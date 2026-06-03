using NodaTime;

namespace FastAddress.Api.Services.Options;

/// <summary>
/// Options for the background job that refreshes stale <c>StreetAddressResult</c> rows. The staleness
/// window itself is taken from <c>AddressSearchOptions.MaxReuseAge</c> so there is a single source of truth.
/// </summary>
public sealed class AddressRefreshOptions
{
    /// <summary>Configuration section key.</summary>
    public const string ConfigKey = "AddressRefresh";

    /// <summary>Maximum number of stale rows to refresh per run.</summary>
    public int BatchSize { get; init; } = 100;

    /// <summary>Hard cap on how long a single run may take before it is canceled.</summary>
    public Duration RunTimeout { get; init; } = Duration.FromMinutes(5);

    /// <summary>
    /// Delay between runs. Read once when the job starts, so a change takes effect only after a restart.
    /// </summary>
    public Duration Interval { get; init; } = Duration.FromHours(4);
}
