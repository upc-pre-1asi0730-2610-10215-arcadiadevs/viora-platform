namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     ServiceProposal read resource (REQ-SP-1..4).
/// </summary>
public record ServiceProposalResource(
    int Id,
    int InterventionRequestId,
    int SpecialistId,
    string ServiceTitle,
    string DurationLabel,
    IReadOnlyList<string> Scope,
    DateOnly ProposedDate,
    decimal CostAmount,
    string CostCurrency,
    string ProposalDetails,
    string Status);
