using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that handles aggregate request-metrics read queries (REQ-OV-3).
/// </summary>
public interface IInterventionRequestMetricsQueryService
{
    Task<InterventionRequestMetrics> Handle(
        GetInterventionRequestMetricsQuery query,
        CancellationToken cancellationToken = default);
}
