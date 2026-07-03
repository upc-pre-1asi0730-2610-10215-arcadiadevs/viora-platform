namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query to get all plots for a user with current imagery attached.
///     Returns <see cref="ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources.PlotWithCurrentImageryResource"/>.
/// </summary>
public record GetPlotsWithCurrentImageryQuery(int UserId);
