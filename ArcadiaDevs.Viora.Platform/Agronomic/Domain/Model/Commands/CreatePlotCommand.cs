using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     Command to create a new plot with geospatial polygon coordinates.
/// </summary>
/// <param name="OwnerUserId">The owner user identifier.</param>
/// <param name="PlotName">The name of the plot.</param>
/// <param name="PolygonCoordinates">The polygon coordinates defining boundaries.</param>
/// <param name="AreaSize">The area size of the plot.</param>
public record CreatePlotCommand(
    int OwnerUserId,
    string PlotName,
    IReadOnlyList<GeoPoint> PolygonCoordinates,
    decimal AreaSize);
