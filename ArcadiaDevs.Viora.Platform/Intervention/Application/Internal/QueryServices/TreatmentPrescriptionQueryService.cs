using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles <see cref="TreatmentPrescription" /> read queries.
/// </summary>
public class TreatmentPrescriptionQueryService(ITreatmentPrescriptionRepository treatmentPrescriptionRepository)
    : ITreatmentPrescriptionQueryService
{
    public async Task<TreatmentPrescription?> Handle(
        GetTreatmentPrescriptionByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await treatmentPrescriptionRepository.FindByIdAsync(query.Id, cancellationToken);
    }

    public async Task<TreatmentPrescription?> Handle(
        GetTreatmentPrescriptionByServiceProposalIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await treatmentPrescriptionRepository.FindByServiceProposalIdAsync(
            query.ServiceProposalId, cancellationToken);
    }
}
