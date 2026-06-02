using FastAddress.Web.Models;

namespace FastAddress.Web.State;

/// <summary>
/// Circuit-scoped state shared between the search box and the map: the location of the most recent
/// selected address, the optional search-location bias, and whether the user is currently picking a
/// bias on the map. Components subscribe to <see cref="Changed"/> to react.
/// </summary>
public sealed class SearchState
{
    /// <summary>Location of the most recently selected search result, if any.</summary>
    public GeoPoint? SearchedLocation { get; private set; }

    /// <summary>The location-bias point applied to searches, if set.</summary>
    public GeoPoint? BiasLocation { get; private set; }

    /// <summary>Whether the user is currently in "set search location" mode (map is draggable).</summary>
    public bool IsSettingLocation { get; private set; }

    /// <summary>Raised after any state change so subscribers can re-render / sync the map.</summary>
    public event Func<Task>? Changed;

    /// <summary>Record the selected address location (the map pans here).</summary>
    /// <param name="location">The selected location.</param>
    public Task SetSearchedLocationAsync(GeoPoint location)
    {
        SearchedLocation = location;
        return NotifyAsync();
    }

    /// <summary>Enter "set search location" mode (enables map dragging + crosshair).</summary>
    public Task StartSettingLocationAsync()
    {
        IsSettingLocation = true;
        return NotifyAsync();
    }

    /// <summary>Leave "set search location" mode without picking a location.</summary>
    public Task StopSettingLocationAsync()
    {
        IsSettingLocation = false;
        return NotifyAsync();
    }

    /// <summary>Set the bias location (picked on the map) and leave set mode.</summary>
    /// <param name="location">The picked bias location.</param>
    public Task SetBiasLocationAsync(GeoPoint location)
    {
        BiasLocation = location;
        IsSettingLocation = false;
        return NotifyAsync();
    }

    /// <summary>Clear the bias location and leave set mode.</summary>
    public Task ClearBiasLocationAsync()
    {
        BiasLocation = null;
        IsSettingLocation = false;
        return NotifyAsync();
    }

    private async Task NotifyAsync()
    {
        if (Changed is null)
        {
            return;
        }

        foreach (var handler in Changed.GetInvocationList().Cast<Func<Task>>())
        {
            await handler();
        }
    }
}
