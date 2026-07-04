using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="ServiceProposal" />.
/// </summary>
public interface IServiceProposalCommandService
{
    /// <summary>
    ///     Submits a new service proposal (REQ-SP-1), validating
    ///     <c>interventionRequestId</c>/<c>specialistId</c> through their
    ///     repositories and side-effecting the parent request to
    ///     <c>PROPOSAL_RECEIVED</c>.
    /// </summary>
    Task<Result<ServiceProposal, Error>> Handle(
        SubmitServiceProposalCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Accepts an existing proposal (REQ-SP-2, self-guarded), side-effecting
    ///     the parent request to <c>ACCEPTED</c>.
    /// </summary>
    Task<Result<ServiceProposal, Error>> Handle(
        AcceptServiceProposalCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Rejects an existing proposal (REQ-SP-3, self-guarded), side-effecting
    ///     the parent request to terminal <c>DECLINED</c>.
    /// </summary>
    Task<Result<ServiceProposal, Error>> Handle(
        RejectServiceProposalCommand command,
        CancellationToken cancellationToken = default);
}
