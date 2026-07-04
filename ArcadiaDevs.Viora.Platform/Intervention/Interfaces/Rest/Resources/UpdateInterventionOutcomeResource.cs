namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for closing an InterventionOutcome with the grower's
///     service evaluation (REQ-IO-2). Self-guarded on the aggregate — only
///     succeeds if the outcome is not already <c>CLOSED</c>.
/// </summary>
public record UpdateInterventionOutcomeResource(
    string? ServiceResult,
    bool HireAgain,
    string? PrivateFeedback);
