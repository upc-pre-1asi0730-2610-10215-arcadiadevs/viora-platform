namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to report the impact of an
///     <see cref="Aggregates.InterventionExecution" />, creating a new
///     <see cref="Aggregates.InterventionOutcome" /> (REQ-IO-1).
///     <c>InterventionExecutionId</c> is validated to exist (REQ-CC-2:
///     missing FK maps to 404). Fails with a conflict (409) if an outcome
///     already exists for the given execution (REQ-IO-3 idempotency).
/// </summary>
public record ReportImpactCommand(
    int InterventionExecutionId,
    string? GracePeriod,
    string? ObservedResult,
    string? ImpactLevel,
    string? ProducerAssessment);
