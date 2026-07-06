namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to submit a <see cref="Aggregates.ServiceProposal" /> for an
///     <see cref="Aggregates.InterventionRequest" /> (REQ-SP-1). Both
///     <c>InterventionRequestId</c> and <c>SpecialistId</c> are validated
///     to exist before the aggregate is constructed (REQ-CC-2: missing FK
///     maps to 404); <c>CostAmount</c>/<c>CostCurrency</c> are validated by
///     <see cref="ValueObjects.CostEstimate" />'s ctor (REQ-CC-2: validation
///     failure maps to 400). Submitting side-effects the parent request to
///     <c>PROPOSAL_RECEIVED</c>.
/// </summary>
public record SubmitServiceProposalCommand(
    int InterventionRequestId,
    int SpecialistId,
    string ServiceTitle,
    string DurationLabel,
    IReadOnlyList<string> Scope,
    DateOnly ProposedDate,
    decimal CostAmount,
    string CostCurrency,
    string ProposalDetails);
