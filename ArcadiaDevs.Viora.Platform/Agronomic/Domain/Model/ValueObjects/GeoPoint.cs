using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Represents a geographic point with latitude and longitude coordinates.
/// </summary>
/// <remarks>
///     Latitude must be between -90 and 90, longitude between -180 and 180.
///     Use the <see cref="Create"/> factory method to validate coordinates.
/// </remarks>
public record GeoPoint
{
    /// <summary>
    ///     Gets the latitude coordinate (-90 to 90).
    /// </summary>
    public decimal Latitude { get; init; }

    /// <summary>
    ///     Gets the longitude coordinate (-180 to 180).
    /// </summary>
    public decimal Longitude { get; init; }

    /// <summary>
    ///     Creates a validated GeoPoint.
    /// </summary>
    /// <param name="latitude">The latitude coordinate.</param>
    /// <param name="longitude">The longitude coordinate.</param>
    /// <returns>A Result containing the GeoPoint if valid, or an error if validation fails.</returns>
    public static Result<GeoPoint, Error> Create(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90)
            return new Result<GeoPoint, Error>.Failure(
                new Error("INVALID_GEOPOINT", "Latitude must be between -90 and 90"));

        if (longitude is < -180 or > 180)
            return new Result<GeoPoint, Error>.Failure(
                new Error("INVALID_GEOPOINT", "Longitude must be between -180 and 180"));

        return new Result<GeoPoint, Error>.Success(
            new GeoPoint { Latitude = latitude, Longitude = longitude });
    }
}