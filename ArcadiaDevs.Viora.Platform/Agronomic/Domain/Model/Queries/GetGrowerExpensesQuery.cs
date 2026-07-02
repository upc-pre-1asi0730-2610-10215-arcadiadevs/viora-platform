namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

/// <summary>
///     Query to retrieve expenses for a grower, optionally filtered by plot.
/// </summary>
public sealed record GetGrowerExpensesQuery(
    long GrowerId,
    long? PlotId = null);
