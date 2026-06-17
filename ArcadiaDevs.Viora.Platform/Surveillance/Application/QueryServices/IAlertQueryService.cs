using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;

public interface IAlertQueryService
{
    Task<Alert?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<AlertSummaryResource>> Handle(GetRecentAlertsByUserIdQuery query, CancellationToken cancellationToken = default);
}
