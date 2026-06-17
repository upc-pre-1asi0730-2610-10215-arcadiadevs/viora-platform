using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.OutboundServices.Acl;

/// <summary>
/// Implementation of the external agronomic service for the anti-corruption layer.
/// </summary>
public class ExternalAgronomicService(IMonitoringSummaryQueryService monitoringSummaryQueryService) : IExternalAgronomicService
{
    /// <inheritdoc/>
    public async Task<double?> FetchCurrentNdviByPlotIdAsync(long plotId, long reporterUserId, CancellationToken cancellationToken = default)
    {
        var query = new GetCurrentMonitoringSummaryQuery((int)reporterUserId); // Assuming reporterUserId is the user id for the query
        var result = await monitoringSummaryQueryService.Handle(query, cancellationToken);
        
        // As a simplification, if the result is successful, return its NDVI
        if (result is Result<MonitoringSummaryDto, Error>.Success success)
        {
            return (double)success.Value.AverageNdvi;
        }

        return null;
    }
}
