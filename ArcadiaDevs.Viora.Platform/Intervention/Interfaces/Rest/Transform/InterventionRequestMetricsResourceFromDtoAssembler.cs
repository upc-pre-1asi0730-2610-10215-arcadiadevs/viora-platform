using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="InterventionRequestMetricsResource" /> from the
///     <see cref="InterventionRequestMetrics" /> DTO.
/// </summary>
public static class InterventionRequestMetricsResourceFromDtoAssembler
{
    public static InterventionRequestMetricsResource ToResourceFromDto(InterventionRequestMetrics dto)
    {
        return new InterventionRequestMetricsResource(
            dto.TotalRequests,
            dto.PendingCount,
            dto.AwaitingResponseCount,
            dto.ProposalReceivedCount,
            dto.AcceptedCount,
            dto.DeclinedCount,
            dto.AcceptanceRate,
            dto.ClosedCount,
            dto.CompletionRate);
    }
}
