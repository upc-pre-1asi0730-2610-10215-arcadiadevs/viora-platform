using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="InterventionExecution" /> aggregates.
/// </summary>
public interface IInterventionExecutionRepository : IBaseRepository<InterventionExecution>
{
    /// <summary>
    ///     Finds the (at most one) execution linked to a given
    ///     <paramref name="treatmentPrescriptionId" /> (REQ-IE-2 idempotency
    ///     guard, also reused for the read-by-prescription REST endpoint).
    /// </summary>
    Task<InterventionExecution?> FindByTreatmentPrescriptionIdAsync(
        int treatmentPrescriptionId,
        CancellationToken cancellationToken = default);
}
