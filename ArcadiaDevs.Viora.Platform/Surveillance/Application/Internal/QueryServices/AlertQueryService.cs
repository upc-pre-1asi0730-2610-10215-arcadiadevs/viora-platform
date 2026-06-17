using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

using ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.QueryServices;

public class AlertQueryService(
    IAlertRepository alertRepository,
    IExternalAgronomicService agronomicService) : IAlertQueryService
{
    public async Task<Alert?> Handle(GetAlertByIdQuery query, CancellationToken cancellationToken = default)
    {
        return await alertRepository.FindByIdAsync((int)query.AlertId, cancellationToken);
    }

    public async Task<IEnumerable<AlertSummaryResource>> Handle(GetRecentAlertsByUserIdQuery query, CancellationToken cancellationToken = default)
    {
        var plotSummaries = await agronomicService.GetPlotsForUserAsMapAsync(query.UserId, cancellationToken);

        if (plotSummaries.Count == 0)
        {
            return Enumerable.Empty<AlertSummaryResource>();
        }

        var plotIds = plotSummaries.Keys.ToList();
        var alertEntities = await alertRepository.FindByPlotIdInOrderByCreatedAtDescAsync(plotIds, query.Limit, cancellationToken);

        return alertEntities.Select(entity =>
        {
            plotSummaries.TryGetValue(entity.PlotId.Value, out var plotSummary);
            
            return new AlertSummaryResource(
                entity.Id,
                entity.Type.ToString(),
                string.IsNullOrEmpty(entity.Title) ? entity.RiskExplanation : entity.Title,
                entity.Severity.ToString(),
                entity.CreatedAt?.ToString("O") ?? string.Empty,
                entity.Status,
                plotSummary!
            );
        });
    }
}
