using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="TreatmentPrescription" />.
/// </summary>
public interface ITreatmentPrescriptionCommandService
{
    /// <summary>
    ///     Creates a new prescription (REQ-TP-1), validating
    ///     <c>ServiceProposalId</c> exists and that no prescription already
    ///     exists for it (REQ-TP-4 idempotency).
    /// </summary>
    Task<Result<TreatmentPrescription, Error>> Handle(
        CreateTreatmentPrescriptionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs the field inspection (REQ-TP-2, self-guarded).
    /// </summary>
    Task<Result<TreatmentPrescription, Error>> Handle(
        LogFieldInspectionCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Issues the agrochemical prescription (REQ-TP-3, self-guarded).
    /// </summary>
    Task<Result<TreatmentPrescription, Error>> Handle(
        PrescribeAgrochemicalCommand command,
        CancellationToken cancellationToken = default);
}
