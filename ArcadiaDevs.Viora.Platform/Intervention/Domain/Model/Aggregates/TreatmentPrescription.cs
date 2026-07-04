using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

/// <summary>
///     The TreatmentPrescription aggregate root — the field-inspection and
///     agrochemical-prescription workflow for an accepted
///     <see cref="ServiceProposal" /> (REQ-TP-1..4). Third link in the FK
///     chain <c>InterventionRequest ← ServiceProposal ← TreatmentPrescription
///     ← ...</c> (REQ-CC-3).
/// </summary>
/// <remarks>
///     <see cref="ServiceProposalId" /> is ctor-only immutable (REQ-CC-3).
///     Creation is NOT guarded by the parent proposal's status (REQ-TP-1,
///     OS parity — an intentional inherited absence, not hardened here).
///     <see cref="Status" /> advances through a self-guarded 3-stage
///     sequence: <c>PENDING_INSPECTION</c> → <c>INSPECTED</c> (
///     <see cref="LogFieldInspection" />) → <c>PRESCRIBED</c> (
///     <see cref="PrescribeAgrochemical" />) — both return
///     <see cref="Result{TValue, TError}" /> rather than throwing, mirroring
///     <c>ServiceProposal.Accept</c>/<c>Reject</c>'s established
///     self-guarded-transition convention (WU4, obs #272 note).
/// </remarks>
public class TreatmentPrescription
{
    public int Id { get; }

    public int ServiceProposalId { get; }

    public TreatmentPrescriptionStatus Status { get; private set; }

    public FieldInspectionRecord? FieldInspectionRecord { get; private set; }

    public AgrochemicalPrescription? AgrochemicalPrescription { get; private set; }

    private TreatmentPrescription()
    {
    }

    public TreatmentPrescription(int serviceProposalId)
    {
        if (serviceProposalId <= 0)
        {
            throw new ArgumentException("Service proposal ID must be positive.", nameof(serviceProposalId));
        }

        ServiceProposalId = serviceProposalId;
        Status = TreatmentPrescriptionStatus.PENDING_INSPECTION;
    }

    /// <summary>
    ///     Logs the field inspection (REQ-TP-2). Self-guarded — only
    ///     succeeds from <c>PENDING_INSPECTION</c>, transitioning to
    ///     <c>INSPECTED</c>.
    /// </summary>
    public Result<Unit, Error> LogFieldInspection(FieldInspectionRecord record)
    {
        if (Status != TreatmentPrescriptionStatus.PENDING_INSPECTION)
        {
            return new Result<Unit, Error>.Failure(InterventionErrors.ConflictError);
        }

        FieldInspectionRecord = record;
        Status = TreatmentPrescriptionStatus.INSPECTED;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Issues the agrochemical prescription (REQ-TP-3). Self-guarded —
    ///     only succeeds from <c>INSPECTED</c>, transitioning to
    ///     <c>PRESCRIBED</c>.
    /// </summary>
    public Result<Unit, Error> PrescribeAgrochemical(AgrochemicalPrescription prescription)
    {
        if (Status != TreatmentPrescriptionStatus.INSPECTED)
        {
            return new Result<Unit, Error>.Failure(InterventionErrors.ConflictError);
        }

        AgrochemicalPrescription = prescription;
        Status = TreatmentPrescriptionStatus.PRESCRIBED;
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
