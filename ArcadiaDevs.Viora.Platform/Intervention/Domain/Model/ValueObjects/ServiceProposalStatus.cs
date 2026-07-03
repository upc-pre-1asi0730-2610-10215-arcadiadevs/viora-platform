namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The lifecycle status of a <see cref="Aggregates.ServiceProposal" />
///     (REQ-SP-1..3). Self-guarded: <c>ACCEPTED</c>/<c>REJECTED</c> are only
///     reachable from <c>PENDING</c> — see
///     <see cref="Aggregates.ServiceProposal.Accept" />/
///     <see cref="Aggregates.ServiceProposal.Reject" />.
/// </summary>
public enum ServiceProposalStatus
{
    PENDING,
    ACCEPTED,
    REJECTED
}
