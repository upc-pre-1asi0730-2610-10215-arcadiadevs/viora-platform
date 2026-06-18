namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query to get a summary overview of all plots owned by a user.
/// </summary>
public record GetMyPlotsOverviewQuery(int UserId);
