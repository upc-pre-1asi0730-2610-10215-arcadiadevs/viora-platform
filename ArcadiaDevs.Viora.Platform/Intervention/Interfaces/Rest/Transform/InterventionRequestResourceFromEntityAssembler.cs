using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="InterventionRequestResource" /> from the
///     <see cref="InterventionRequest" /> aggregate — input is a genuine
///     entity (not a read-model DTO), hence the <c>FromEntity</c> naming
///     (distinct from Specialist's <c>FromDto</c> assembler, see that
///     type's remarks).
/// </summary>
public static class InterventionRequestResourceFromEntityAssembler
{
    public static InterventionRequestResource ToResourceFromEntity(InterventionRequest entity)
    {
        return new InterventionRequestResource(
            entity.Id,
            entity.GrowerId,
            entity.PlotId,
            entity.SpecialistId,
            entity.AlertId,
            entity.Reason,
            entity.Message,
            entity.Status.ToString(),
            entity.DeclineReason);
    }
}
