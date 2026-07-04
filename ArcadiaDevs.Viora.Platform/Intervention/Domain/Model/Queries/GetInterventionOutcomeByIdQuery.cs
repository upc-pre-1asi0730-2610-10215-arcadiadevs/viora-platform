namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to retrieve an <see cref="Aggregates.InterventionOutcome" />
///     by its id (REQ-IO-4).
/// </summary>
public record GetInterventionOutcomeByIdQuery(int Id);
