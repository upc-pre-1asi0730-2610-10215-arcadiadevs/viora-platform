namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for partially updating a ServiceProposal (REQ-SP-2,
///     REQ-SP-3). The only supported target <paramref name="Status" />
///     values are <c>ACCEPTED</c> and <c>REJECTED</c>; both are
///     self-guarded on the aggregate (PENDING only, 409 otherwise).
/// </summary>
/// <param name="Status">The target status. <c>ACCEPTED</c> or <c>REJECTED</c>.</param>
public record UpdateServiceProposalResource(string? Status);
