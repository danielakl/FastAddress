using FastAddress.Api.Services.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FastAddress.Api.Services.BackgroundServices;

/// <summary>
/// Periodically refreshes the most stale street-address results. A thin scheduler that owns the run
/// interval and resolves <see cref="IAddressRefreshService"/>.
/// Errors are logged so one bad run never stops the loop.
/// </summary>
internal sealed partial class AddressRefreshBackgroundService(
    IServiceScopeFactory scopes,
    IOptionsMonitor<AddressRefreshOptions> options,
    ILogger<AddressRefreshBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The interval is fixed once the timer is created, so changing it takes effect after a restart.
        var interval = options.CurrentValue.Interval.ToTimeSpan();
        using var timer = new PeriodicTimer(interval);

        // Run once at startup, then on every tick.
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            LogCheckingForStaleAddresses(logger);

            using var scope = scopes.CreateScope();
            var refresher = scope.ServiceProvider.GetRequiredService<IAddressRefreshService>();
            await refresher.RefreshStaleAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Application shutdown. Let the loop unwind.
            throw;
        }
        catch (Exception ex)
        {
            // Swallow-and-log so a failed run never kills the schedule.
            LogRunFailed(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Checking for stale addresses to refresh")]
    private static partial void LogCheckingForStaleAddresses(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stale address refresh run failed")]
    private static partial void LogRunFailed(ILogger logger, Exception exception);
}
