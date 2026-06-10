using System.ComponentModel.DataAnnotations;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Request DTO for creating a new plot.
/// </summary>
/// <param name="OwnerUserId">The owner user identifier.</param>
/// <param name="PlotName">The name of the plot.</param>
/// <param name="PolygonCoordinates">The polygon coordinates defining boundaries.</param>
/// <param name="AreaSize">The area size of the plot (must be positive).</param>
public record CreatePlotResource(
    [Range(1, int.MaxValue)] int OwnerUserId,
    [Required, StringLength(256, MinimumLength = 1)] string PlotName,
    [Required, MinLength(4)] List<GeoPointResource> PolygonCoordinates,
    [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
    decimal AreaSize);

/// <summary>
///     DTO for a geographic point within polygon coordinates.
/// </summary>
/// <param name="Latitude">The latitude coordinate.</param>
/// <param name="Longitude">The longitude coordinate.</param>
public record GeoPointResource(
    [Range(typeof(decimal), "-90", "90")] decimal Latitude,
    [Range(typeof(decimal), "-180", "180")] decimal Longitude);
