using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Represents a polygon defined by a list of geographic points.
/// </summary>
/// <remarks>
///     The polygon must be closed (first point equals last point) and contain at least 4 points.
///     Use the <see cref="Create"/> factory method to validate coordinates.
/// </remarks>
public record PolygonCoordinates
{
    /// <summary>
    ///     Gets the list of points defining the polygon.
    /// </summary>
    public IReadOnlyList<GeoPoint> Points { get; init; } = [];

    /// <summary>
    ///     Creates validated polygon coordinates.
    /// </summary>
    /// <param name="points">The list of geographic points.</param>
    /// <returns>A Result containing the PolygonCoordinates if valid, or an error if validation fails.</returns>
    public static Result<PolygonCoordinates, Error> Create(IReadOnlyList<GeoPoint> points)
    {
        if (points is null || points.Count == 0)
            return new Result<PolygonCoordinates, Error>.Failure(
                new Error("INVALID_POLYGON", "Polygon must contain at least one point"));

        if (points.Count < 4)
            return new Result<PolygonCoordinates, Error>.Failure(
                new Error("INVALID_POLYGON", "Polygon must contain at least 4 points, including the closing point"));

        if (points.Any(p => p is null))
            return new Result<PolygonCoordinates, Error>.Failure(
                new Error("INVALID_POLYGON", "Polygon contains null points"));

        if (points.Any(p => p.Latitude is < -90 or > 90 || p.Longitude is < -180 or > 180))
            return new Result<PolygonCoordinates, Error>.Failure(
                new Error("INVALID_GEOPOINT", "Latitude must be between -90 and 90 and longitude between -180 and 180"));

        // Check if polygon is closed (first point equals last point)
        var first = points[0];
        var last = points[^1];
        if (first.Latitude != last.Latitude || first.Longitude != last.Longitude)
            return new Result<PolygonCoordinates, Error>.Failure(
                new Error("INVALID_POLYGON", "Polygon must be closed (first point equals last point)"));

        if (points.Take(points.Count - 1).Distinct().Count() < 3)
            return new Result<PolygonCoordinates, Error>.Failure(
                new Error("INVALID_POLYGON", "Polygon must contain at least 3 distinct vertices"));

        return new Result<PolygonCoordinates, Error>.Success(
            new PolygonCoordinates { Points = points.ToList().AsReadOnly() });
    }
}
