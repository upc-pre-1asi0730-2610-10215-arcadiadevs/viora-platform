namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record UpdateInterventionOutcomeResource(
    string? ServiceResult,
    bool HireAgain,
    string? PrivateFeedback);
