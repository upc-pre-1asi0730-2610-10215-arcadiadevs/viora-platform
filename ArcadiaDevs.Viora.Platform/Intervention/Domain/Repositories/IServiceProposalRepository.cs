using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="ServiceProposal" /> aggregates.
/// </summary>
public interface IServiceProposalRepository : IBaseRepository<ServiceProposal>
{
    /// <summary>
    ///     Lists proposals for an intervention request (REQ-SP-4).
    /// </summary>
    Task<IReadOnlyList<ServiceProposal>> ListByInterventionRequestIdAsync(
        int interventionRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists all proposals submitted by a specialist
    ///     (specialist-dashboard-parity — mirrors OS's
    ///     <c>findBySpecialistId</c>).
    /// </summary>
    Task<IReadOnlyList<ServiceProposal>> FindBySpecialistIdAsync(
        int specialistId,
        CancellationToken cancellationToken = default);
}
