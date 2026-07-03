namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to list all <see cref="Aggregates.ServiceProposal" />s for an
///     intervention request (REQ-SP-4).
/// </summary>
public record ListServiceProposalsByRequestQuery(int InterventionRequestId);
