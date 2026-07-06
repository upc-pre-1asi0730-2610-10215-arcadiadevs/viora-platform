namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to list all <see cref="Aggregates.ServiceProposal" />s for an
///     intervention request (REQ-SP-4).
/// </summary>
/// <param name="InterventionRequestId">The intervention request id.</param>
/// <param name="GrowerId">The authenticated caller's id, derived from the token — enforced as the request's owner.</param>
public record ListServiceProposalsByRequestQuery(int InterventionRequestId, int GrowerId);
