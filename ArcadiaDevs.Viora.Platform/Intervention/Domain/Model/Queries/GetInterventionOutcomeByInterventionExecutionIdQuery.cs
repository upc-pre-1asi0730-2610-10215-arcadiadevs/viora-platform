namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to retrieve the (at most one) <see cref="Aggregates.InterventionOutcome" />
///     linked to a given <c>InterventionExecutionId</c> (REQ-IO-3 idempotency
///     lookup, reused as a read endpoint — mirrors
///     <c>InterventionExecution</c>'s find-by-parent-id read shape, WU6).
/// </summary>
public record GetInterventionOutcomeByInterventionExecutionIdQuery(int InterventionExecutionId);
