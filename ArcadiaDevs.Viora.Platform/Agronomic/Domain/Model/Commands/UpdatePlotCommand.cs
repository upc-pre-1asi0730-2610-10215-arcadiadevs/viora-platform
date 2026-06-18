using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     Command to update an existing plot.
/// </summary>
/// <param name="PlotId">The plot identifier.</param>
/// <param name="Name">The new name for the plot.</param>
/// <param name="CropType">The new crop type.</param>
/// <param name="Variety">The new crop variety.</param>
/// <param name="Location">The new plot location.</param>
/// <param name="Campaign">The new production campaign.</param>
/// <param name="Notes">The new grower notes.</param>
/// <param name="PolygonCoordinates">The new boundary coordinates, if updated.</param>
public record UpdatePlotCommand(
    int PlotId,
    string? Name,
    string? CropType,
    string? Variety,
    string? Location,
    string? Campaign,
    string? Notes,
    IReadOnlyList<GeoPoint>? PolygonCoordinates);
