using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="InterventionOutcomeResource" /> from the
///     <see cref="InterventionOutcome" /> aggregate — mirrors
///     <c>InterventionExecutionResourceFromEntityAssembler</c>'s
///     <c>FromEntity</c> naming.
/// </summary>
public static class InterventionOutcomeResourceFromEntityAssembler
{
    public static InterventionOutcomeResource ToResourceFromEntity(InterventionOutcome entity)
    {
        return new InterventionOutcomeResource(
            entity.Id,
            entity.InterventionExecutionId,
            entity.Status.ToString(),
            entity.ImpactReport.GracePeriod,
            entity.ImpactReport.ObservedResult,
            entity.ImpactReport.ImpactLevel,
            entity.ImpactReport.ProducerAssessment,
            entity.ServiceEvaluation?.ServiceResult,
            entity.ServiceEvaluation?.HireAgain,
            entity.ServiceEvaluation?.PrivateFeedback);
    }
}
