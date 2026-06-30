using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;

public interface IAlertQueryService
{
    Task<Alert?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<AlertSummaryResource>> Handle(GetRecentAlertsByUserIdQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    ///     SURV-003: handles the sort-key-aware <see cref="GetAlertsByUserIdQuery"/>
    ///     (replaces the empty-list placeholder for non-<c>"recent"</c> sorts).
    ///     Returns an empty list (never null) when no alerts exist for the user.
    /// </summary>
    Task<IEnumerable<AlertSummaryResource>> Handle(GetAlertsByUserIdQuery query, CancellationToken cancellationToken = default);
}
