using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;

public interface IAlertQueryService
{
    Task<Alert?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken = default);
}
