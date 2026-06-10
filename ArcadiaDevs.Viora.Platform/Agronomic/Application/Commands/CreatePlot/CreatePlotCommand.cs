using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates.Plot;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Cortex.Mediator.Commands;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Commands.CreatePlot;

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
    List<GeoPoint> PolygonCoordinates,
    decimal AreaSize) : ICommand<Result<Plot, Error>>;