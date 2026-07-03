namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query to get the weather forecast for a specific plot.
/// </summary>
public record GetPlotWeatherForecastQuery(int PlotId, int UserId);
