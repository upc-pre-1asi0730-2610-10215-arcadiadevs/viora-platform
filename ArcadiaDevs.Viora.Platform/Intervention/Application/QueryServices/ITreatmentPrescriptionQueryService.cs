using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that handles read queries for <see cref="TreatmentPrescription" />.
/// </summary>
public interface ITreatmentPrescriptionQueryService
{
    Task<TreatmentPrescription?> Handle(
        GetTreatmentPrescriptionByIdQuery query,
        CancellationToken cancellationToken = default);

    Task<TreatmentPrescription?> Handle(
        GetTreatmentPrescriptionByServiceProposalIdQuery query,
        CancellationToken cancellationToken = default);
}
