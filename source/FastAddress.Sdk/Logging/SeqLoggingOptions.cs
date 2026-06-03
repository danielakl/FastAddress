namespace FastAddress.Sdk.Logging;

/// <summary>
/// Strongly typed Seq logging configuration.
/// </summary>
public sealed class SeqLoggingOptions
{
    /// <summary>Configuration section these options bind from.</summary>
    public const string ConfigKey = "Seq";

    /// <summary>Seq ingestion URL. Leave unset to disable the Seq sink.</summary>
    public Uri? Url { get; init; }
}
