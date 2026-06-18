namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query to get a specific NDVI tile for a plot.
/// </summary>
/// <param name="UserId">The ID of the user requesting the tile.</param>
/// <param name="PlotId">The ID of the plot.</param>
/// <param name="Zoom">The zoom level (Z).</param>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
public record GetPlotNdviTileQuery(int UserId, int PlotId, int Zoom, int X, int Y);
