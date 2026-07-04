using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="InterventionExecutionResource" /> from the
///     <see cref="InterventionExecution" /> aggregate — mirrors
///     <c>TreatmentPrescriptionResourceFromEntityAssembler</c>'s
///     <c>FromEntity</c> naming.
/// </summary>
public static class InterventionExecutionResourceFromEntityAssembler
{
    public static InterventionExecutionResource ToResourceFromEntity(InterventionExecution entity)
    {
        return new InterventionExecutionResource(
            entity.Id,
            entity.TreatmentPrescriptionId,
            entity.ApplicationDate,
            entity.AppliedArea,
            entity.ExecutionStatus.ToString(),
            entity.FieldNote);
    }
}
