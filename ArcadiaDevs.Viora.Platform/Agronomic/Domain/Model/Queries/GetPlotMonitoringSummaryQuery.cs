namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query to get the monitoring summary of a specific plot.
/// </summary>
public record GetPlotMonitoringSummaryQuery(int PlotId, int UserId);
