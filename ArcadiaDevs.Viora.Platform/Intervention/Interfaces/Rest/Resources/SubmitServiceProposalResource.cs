namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for submitting a ServiceProposal (REQ-SP-1).
/// </summary>
public record SubmitServiceProposalResource(
    int InterventionRequestId,
    int SpecialistId,
    string ServiceTitle,
    string DurationLabel,
    IReadOnlyList<string> Scope,
    DateOnly ProposedDate,
    decimal CostAmount,
    string CostCurrency,
    string ProposalDetails);
