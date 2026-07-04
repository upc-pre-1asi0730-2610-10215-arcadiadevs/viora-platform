namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to retrieve an <see cref="Aggregates.InterventionExecution" />
///     by its id.
/// </summary>
public record GetInterventionExecutionByIdQuery(int Id);
