using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="TreatmentPrescription" /> aggregates.
/// </summary>
public interface ITreatmentPrescriptionRepository : IBaseRepository<TreatmentPrescription>
{
    /// <summary>
    ///     Finds the (at most one) prescription linked to a given
    ///     <paramref name="serviceProposalId" /> (REQ-TP-4 idempotency guard,
    ///     also reused for the read-by-proposal REST endpoint).
    /// </summary>
    Task<TreatmentPrescription?> FindByServiceProposalIdAsync(
        int serviceProposalId,
        CancellationToken cancellationToken = default);
}
