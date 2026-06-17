using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.OutboundServices.Acl;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.OutboundServices.Acl;

public class ExternalAgronomicService(IMonitoringSummaryQueryService monitoringSummaryQueryService) : IExternalAgronomicService
{
    public async Task<double?> FetchCurrentNdviByPlotIdAsync(long plotId, long reporterUserId, CancellationToken cancellationToken = default)
    {
        var query = new GetCurrentMonitoringSummaryQuery((int)reporterUserId); // Assuming reporterUserId is the user id for the query
        var result = await monitoringSummaryQueryService.Handle(query, cancellationToken);
        
        // As a simplification, if the result is successful, return its NDVI
        if (result.IsSuccess && result.Value != null)
        {
            return (double)result.Value.AverageNdvi;
        }

        return null;
    }
}
