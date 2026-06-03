using System.Globalization;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Refit.Destructurers;

namespace FastAddress.Sdk.Logging;

/// <summary>
/// Wires Serilog into the host with the shared FastAddress logging conventions, so every host
/// configures logging the same way from one place. Enriches with the log context, the environment
/// name, the entry-assembly version, the current activity, and structured exception details.
/// </summary>
public static class SerilogHostBuilderExtensions
{
    /// <summary>Configure Serilog as the logging provider for <paramref name="hostBuilder"/>.</summary>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <returns>The same <paramref name="hostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseFastAddressSerilog(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder.UseSerilog(ConfigureLogger);
    }

    private static void ConfigureLogger(
        HostBuilderContext context,
        IServiceProvider services,
        LoggerConfiguration loggerConfiguration)
    {
        var configuration = context.Configuration;
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
            .Enrich.WithProperty("RoleInstance", Environment.MachineName)
            .Enrich.WithProperty("Version", version)
            .Enrich.With<ActivityEnricher>()
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                .WithDefaultDestructurers()
                .WithDestructurers([new ApiExceptionDestructurer()]))
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

        var seqOptions = services.GetRequiredService<IOptions<SeqLoggingOptions>>().Value;
        if (seqOptions.Url is not null)
        {
            loggerConfiguration.WriteTo.Seq(serverUrl: seqOptions.Url.ToString(), formatProvider: CultureInfo.InvariantCulture);
        }
    }
}
