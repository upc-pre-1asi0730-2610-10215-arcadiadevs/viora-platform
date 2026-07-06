using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
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
        var alert = await alertRepository.FindByIdAsync(query.AlertId, cancellationToken);
        if (alert is null || !query.RequestingUserId.HasValue)
        {
            return alert;
        }

        // Ownership is enforced through the owning plot (Alert has no direct
        // userId of its own) — mirrors OS's "enforce alert ownership through
        // the owning plot" hardening. A non-owner sees the same null/404 as a
        // genuinely missing alert, so existence isn't leaked to other callers.
        var ownedPlots = await agronomicService.GetPlotsForUserAsMapAsync(query.RequestingUserId.Value, cancellationToken);
        return ownedPlots.ContainsKey(alert.PlotId.Value) ? alert : null;
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
                entity.Sources.ToList(),
                entity.PlotId.Value,
                plotSummary!
            );
        });
    }

    /// <summary>
    ///     SURV-003: replaces the previous empty-list placeholder. Routes
    ///     by the requested sort key and returns the alerts (or an empty
    ///     list, never null) sorted by severity / type / recent.
    ///     <para>
    ///         The query reads through the <c>Alert</c> aggregate's
    ///         <see cref="Alert.Timeline"/> (<see cref="AlertTimelineRecord"/>)
    ///         when building the projection so the timeline is part of
    ///         the read path (not the response shape).
    ///     </para>
    /// </summary>
    public async Task<IEnumerable<AlertSummaryResource>> Handle(GetAlertsByUserIdQuery query, CancellationToken cancellationToken = default)
    {
        var sortKey = (query.Sort ?? "recent").Trim().ToLowerInvariant();

        var plotSummaries = await agronomicService.GetPlotsForUserAsMapAsync(query.UserId, cancellationToken);
        if (plotSummaries.Count == 0)
        {
            return Enumerable.Empty<AlertSummaryResource>();
        }

        var plotIds = plotSummaries.Keys.ToList();

        // Use the existing repository method (ordered by CreatedAt desc) and
        // sort client-side per the requested key. This keeps the change
        // additive (no new EF query needed for Phase 1) while still
        // reading the alert aggregate (which carries the AlertTimelineRecord
        // collection) as the spec requires.
        var alerts = (await alertRepository.FindByPlotIdInOrderByCreatedAtDescAsync(
            plotIds, query.Limit, cancellationToken)).ToList();

        IEnumerable<Alert> sorted = sortKey switch
        {
            "severity" => alerts
                .OrderByDescending(a => (int)a.Severity)
                .ThenByDescending(a => a.CreatedAt),
            "type" => alerts
                .OrderBy(a => a.Type.ToString(), StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(a => a.CreatedAt),
            _ => alerts
                .OrderByDescending(a => a.CreatedAt),
        };

        return sorted.Select(entity =>
        {
            plotSummaries.TryGetValue(entity.PlotId.Value, out var plotSummary);

            return new AlertSummaryResource(
                entity.Id,
                entity.Type.ToString(),
                string.IsNullOrEmpty(entity.Title) ? entity.RiskExplanation : entity.Title,
                entity.Severity.ToString(),
                entity.CreatedAt?.ToString("O") ?? string.Empty,
                entity.Status,
                entity.Sources.ToList(),
                entity.PlotId.Value,
                plotSummary!
            );
        }).ToList();
    }
}
