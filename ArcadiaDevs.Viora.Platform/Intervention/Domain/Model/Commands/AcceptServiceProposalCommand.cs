namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to accept a <see cref="Aggregates.ServiceProposal" />
///     (REQ-SP-2). Self-guarded — only succeeds from <c>PENDING</c>
///     (409 otherwise). Side-effects the parent
///     <see cref="Aggregates.InterventionRequest" /> to <c>ACCEPTED</c>.
/// </summary>
/// <param name="Id">The service proposal id.</param>
/// <param name="GrowerId">The authenticated caller's id, derived from the token — enforced as the parent request's owner.</param>
public record AcceptServiceProposalCommand(int Id, int GrowerId);
