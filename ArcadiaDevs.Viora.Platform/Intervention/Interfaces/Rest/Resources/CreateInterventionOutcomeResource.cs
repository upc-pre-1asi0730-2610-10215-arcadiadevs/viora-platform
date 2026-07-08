namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record CreateInterventionOutcomeResource(
    int InterventionExecutionId,
    string? GracePeriod,
    string? ObservedResult,
    string? ImpactLevel,
    string? ProducerAssessment);
