namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The lifecycle status of an <see cref="Aggregates.InterventionOutcome" />
///     (REQ-IO-1..2). Self-guarded single transition:
///     <c>IMPACT_REPORTED</c> → <c>CLOSED</c> (via
///     <see cref="Aggregates.InterventionOutcome.CloseWithEvaluation" />),
///     with no re-close (REQ-IO-2).
/// </summary>
public enum InterventionOutcomeStatus
{
    IMPACT_REPORTED,
    CLOSED
}
