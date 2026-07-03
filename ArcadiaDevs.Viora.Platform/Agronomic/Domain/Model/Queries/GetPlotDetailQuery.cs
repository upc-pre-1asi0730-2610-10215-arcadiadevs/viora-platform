namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query to get detailed information of a specific plot.
/// </summary>
public record GetPlotDetailQuery(int PlotId, int UserId);
