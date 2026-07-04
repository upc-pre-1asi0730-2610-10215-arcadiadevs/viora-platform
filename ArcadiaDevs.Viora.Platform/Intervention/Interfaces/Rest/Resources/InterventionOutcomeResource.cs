namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     InterventionOutcome read resource (REQ-IO-1..4). <c>serviceResult</c>,
///     <c>hireAgain</c>, and <c>privateFeedback</c> are null until the
///     outcome is closed (REQ-IO-2).
/// </summary>
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
