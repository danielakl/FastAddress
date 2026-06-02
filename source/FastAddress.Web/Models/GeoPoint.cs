namespace FastAddress.Web.Models;

/// <summary>
/// A simple geographic point used to pass coordinates between components (e.g. the map's current
/// center supplied to the search as a location bias).
/// </summary>
/// <param name="Latitude">Latitude in degrees (WGS84).</param>
/// <param name="Longitude">Longitude in degrees (WGS84).</param>
public readonly record struct GeoPoint(double Latitude, double Longitude);
