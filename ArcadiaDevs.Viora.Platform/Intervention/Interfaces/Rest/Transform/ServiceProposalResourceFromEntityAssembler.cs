using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="ServiceProposalResource" /> from the
///     <see cref="ServiceProposal" /> aggregate — mirrors
///     <c>InterventionRequestResourceFromEntityAssembler</c>'s
///     <c>FromEntity</c> naming.
/// </summary>
public static class ServiceProposalResourceFromEntityAssembler
{
    public static ServiceProposalResource ToResourceFromEntity(ServiceProposal entity)
    {
        return new ServiceProposalResource(
            entity.Id,
            entity.InterventionRequestId,
            entity.SpecialistId,
            entity.ServiceTitle,
            entity.DurationLabel,
            entity.Scope,
            entity.ProposedDate,
            entity.CostEstimate.Amount,
            entity.CostEstimate.Currency,
            entity.ProposalDetails,
            entity.Status.ToString());
    }
}
