namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to reject a <see cref="Aggregates.ServiceProposal" />
///     (REQ-SP-3). Self-guarded — only succeeds from <c>PENDING</c> (409
///     otherwise). Side-effects the parent
///     <see cref="Aggregates.InterventionRequest" /> to terminal
///     <c>DECLINED</c> — no re-routing, no re-opening (locked business
///     logic, not a gap).
/// </summary>
/// <param name="Id">The service proposal id.</param>
/// <param name="GrowerId">The authenticated caller's id, derived from the token — enforced as the parent request's owner.</param>
public record RejectServiceProposalCommand(int Id, int GrowerId);
