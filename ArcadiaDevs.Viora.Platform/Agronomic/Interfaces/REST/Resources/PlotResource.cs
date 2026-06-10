namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Response DTO for a created plot.
/// </summary>
/// <param name="PlotId">The plot identifier.</param>
/// <param name="OwnerUserId">The owner user identifier.</param>
/// <param name="PlotName">The name of the plot.</param>
/// <param name="PolygonCoordinates">The polygon coordinates defining boundaries.</param>
/// <param name="AreaSize">The area size of the plot.</param>
/// <param name="CreatedAt">The UTC timestamp when the plot was created.</param>
public record PlotResource(
    int PlotId,
    int OwnerUserId,
    string PlotName,
    List<GeoPointResource> PolygonCoordinates,
    decimal AreaSize,
    DateTimeOffset CreatedAt);
