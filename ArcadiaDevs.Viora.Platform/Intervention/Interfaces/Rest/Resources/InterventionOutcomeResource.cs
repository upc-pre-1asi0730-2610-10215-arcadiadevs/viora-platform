namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record InterventionOutcomeResource(
    int Id,
    int InterventionExecutionId,
    string Status,
    string GracePeriod,
    string ObservedResult,
    string ImpactLevel,
    string ProducerAssessment,
    string? ServiceResult,
    bool? HireAgain,
    string? PrivateFeedback);
