using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

/// <summary>
///     The InterventionExecution aggregate root — certifies that a
///     <see cref="TreatmentPrescription" />'s agrochemical prescription was
///     applied in the field (REQ-IE-1..3). Fourth link in the FK chain
///     <c>InterventionRequest ← ServiceProposal ← TreatmentPrescription ←
///     InterventionExecution ← ...</c> (REQ-CC-3).
/// </summary>
/// <remarks>
///     <see cref="TreatmentPrescriptionId" /> is ctor-only immutable
///     (REQ-CC-3). Unlike <see cref="TreatmentPrescription" />'s 3-stage
///     self-guarded sequence, this aggregate has no in-aggregate transitions
///     — every field is supplied at certification time via a single ctor
///     call. The REQ-IE-1 guard (parent prescription must be
///     <c>PRESCRIBED</c>) is enforced at the command-service level, NOT as
///     a ctor invariant here, since an aggregate cannot reach into a
///     sibling aggregate to validate itself (design decision 3, obs #267).
/// </remarks>
public class InterventionExecution
{
    public int Id { get; }

    public int TreatmentPrescriptionId { get; }

    public DateOnly ApplicationDate { get; }

    public decimal AppliedArea { get; }

    public InterventionExecutionStatus ExecutionStatus { get; }

    public string? FieldNote { get; }

    private InterventionExecution()
    {
    }

    public InterventionExecution(
        int treatmentPrescriptionId,
        DateOnly applicationDate,
        decimal appliedArea,
        string executionStatus,
        string? fieldNote)
    {
        if (treatmentPrescriptionId <= 0)
        {
            throw new ArgumentException("Treatment prescription ID must be positive.", nameof(treatmentPrescriptionId));
        }

        if (appliedArea < 0)
        {
            throw new ArgumentException("Applied area cannot be negative.", nameof(appliedArea));
        }

        TreatmentPrescriptionId = treatmentPrescriptionId;
        ApplicationDate = applicationDate;
        AppliedArea = appliedArea;
        ExecutionStatus = Enum.Parse<InterventionExecutionStatus>(executionStatus, true);
        FieldNote = fieldNote;
    }
}
