namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     Command to delete an existing plot.
/// </summary>
/// <param name="PlotId">The plot identifier.</param>
public record DeletePlotCommand(int PlotId);
