using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="InterventionOutcome" /> aggregates.
/// </summary>
public interface IInterventionOutcomeRepository : IBaseRepository<InterventionOutcome>
{
    /// <summary>
    ///     Finds the (at most one) outcome linked to a given
    ///     <paramref name="interventionExecutionId" /> (REQ-IO-3 idempotency
    ///     guard, also reused for the read-by-execution REST endpoint).
    /// </summary>
    Task<InterventionOutcome?> FindByInterventionExecutionIdAsync(
        int interventionExecutionId,
        CancellationToken cancellationToken = default);
}
