namespace FastAddress.Api.Options;

/// <summary>Options for database setup.</summary>
public sealed record DatabaseOptions
{
    /// <summary>Expected key to find in the configuration store.</summary>
    public const string ConfigKey = "Database";

    /// <summary>Connection string used to connect to the database.</summary>
    /// <remarks>Will contain sensitive information such as username and password.</remarks>
    public required string ConnectionString { get; init; }
    
    /// <summary>
    /// Whether to enable detailed errors when handling of data value exceptions that occur during processing of store query results.
    /// Such errors most often occur due to misconfiguration of entity properties. E.g. If a property is configured
    /// to be of type 'int', but the underlying data in the store is actually of type 'string',
    /// then an exception will be generated at runtime during processing of the data value.
    /// When this option is enabled and a data error is encountered, the generated exception will include
    /// details of the specific entity property that generated the error.
    /// </summary>
    public bool EnableDetailedErrors { get; init; }
    
    /// <summary>
    /// Whether to enable application data to be included in exception messages, logging, etc.
    /// This can include the values assigned to properties of your entity instances, parameter values for
    /// commands being sent to the database, and other such data.
    /// You should only enable this flag if you have the appropriate security measures in place based on
    /// the sensitivity of this data.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; init; }
    
};
