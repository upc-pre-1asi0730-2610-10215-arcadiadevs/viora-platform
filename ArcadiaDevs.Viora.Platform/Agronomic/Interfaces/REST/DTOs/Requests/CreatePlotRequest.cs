using System.ComponentModel.DataAnnotations;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.DTOs.Requests;

/// <summary>
///     Request DTO for creating a new plot.
/// </summary>
/// <param name="OwnerUserId">The owner user identifier.</param>
/// <param name="PlotName">The name of the plot.</param>
/// <param name="PolygonCoordinates">The polygon coordinates defining boundaries.</param>
/// <param name="AreaSize">The area size of the plot (must be positive).</param>
public record CreatePlotRequest(
    [Required] int OwnerUserId,
    [Required] string PlotName,
    [Required] List<GeoPointDto> PolygonCoordinates,
    [Required] decimal AreaSize);

/// <summary>
///     DTO for a geographic point within polygon coordinates.
/// </summary>
/// <param name="Latitude">The latitude coordinate.</param>
/// <param name="Longitude">The longitude coordinate.</param>
public record GeoPointDto(decimal Latitude, decimal Longitude);