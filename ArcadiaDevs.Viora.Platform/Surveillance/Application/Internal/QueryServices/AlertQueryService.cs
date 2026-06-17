using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.QueryServices;

public class AlertQueryService(IAlertRepository alertRepository) : IAlertQueryService
{
    public async Task<Alert?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await alertRepository.FindByIdAsync((int)query.AlertId, cancellationToken);
    }
}
