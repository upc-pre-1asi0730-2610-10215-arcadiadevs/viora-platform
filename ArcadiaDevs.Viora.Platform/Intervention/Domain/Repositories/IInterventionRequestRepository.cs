using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="InterventionRequest" /> aggregates.
/// </summary>
public interface IInterventionRequestRepository : IBaseRepository<InterventionRequest>
{
    /// <summary>
    ///     Lists requests for a grower (REQ-IREQ-2), optionally narrowed to
    ///     a single plot.
    /// </summary>
    Task<IReadOnlyList<InterventionRequest>> ListByGrowerIdAsync(
        int growerId,
        long? plotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists requests assigned to a specialist, filtered by status
    ///     (specialist-dashboard-parity — mirrors OS's
    ///     <c>findBySpecialistIdAndStatus</c>).
    /// </summary>
    Task<IReadOnlyList<InterventionRequest>> FindBySpecialistIdAndStatusAsync(
        int specialistId,
        InterventionStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists every request assigned to a specialist, unfiltered by status
    ///     (marketplace-and-cases-read-models — mirrors OS's
    ///     <c>findBySpecialistId</c>) — distinct from
    ///     <see cref="FindBySpecialistIdAndStatusAsync" />.
    /// </summary>
    Task<IReadOnlyList<InterventionRequest>> FindBySpecialistIdAsync(
        int specialistId,
        CancellationToken cancellationToken = default);
}
