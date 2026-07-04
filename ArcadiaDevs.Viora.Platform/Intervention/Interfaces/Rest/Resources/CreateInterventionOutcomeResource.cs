namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for reporting the impact of an InterventionExecution,
///     creating a new InterventionOutcome (REQ-IO-1).
/// </summary>
public record CreateInterventionOutcomeResource(
    int InterventionExecutionId,
    string? GracePeriod,
    string? ObservedResult,
    string? ImpactLevel,
    string? ProducerAssessment);
