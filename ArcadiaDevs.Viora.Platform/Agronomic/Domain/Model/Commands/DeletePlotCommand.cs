namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     Command to delete an existing plot.
/// </summary>
/// <param name="PlotId">The plot identifier.</param>
/// <param name="UserId">The authenticated caller's id, derived from the token.</param>
public record DeletePlotCommand(int PlotId, int UserId);
