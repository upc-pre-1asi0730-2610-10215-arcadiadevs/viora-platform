namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query to get a specific plot by its ID.
/// </summary>
/// <param name="PlotId">The plot identifier.</param>
/// <param name="UserId">The authenticated caller's id, derived from the token.</param>
public record GetPlotByIdQuery(int PlotId, int UserId);
