using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
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
}
