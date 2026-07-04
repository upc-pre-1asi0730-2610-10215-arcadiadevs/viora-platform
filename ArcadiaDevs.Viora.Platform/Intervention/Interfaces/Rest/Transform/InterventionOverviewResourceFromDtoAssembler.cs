using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="InterventionOverviewResource" /> from the
///     <see cref="InterventionOverviewItem" /> DTO — mirrors
///     <c>SpecialistResourceFromDtoAssembler</c>'s <c>FromDto</c> naming
///     (WU8 is a composed read model, not a single-aggregate entity, so it
///     assembles from a DTO rather than from an entity directly).
/// </summary>
public static class InterventionOverviewResourceFromDtoAssembler
{
    public static InterventionOverviewResource ToResourceFromDto(InterventionOverviewItem dto)
    {
        return new InterventionOverviewResource(
            dto.InterventionRequestId,
            dto.GrowerId,
            dto.PlotId,
            dto.SpecialistId,
            dto.RequestStatus,
            dto.ServiceProposalId,
            dto.ProposalStatus,
            dto.TreatmentPrescriptionId,
            dto.PrescriptionStatus,
            dto.InterventionExecutionId,
            dto.ExecutionStatus,
            dto.InterventionOutcomeId,
            dto.OutcomeStatus,
            dto.Status);
    }
}
