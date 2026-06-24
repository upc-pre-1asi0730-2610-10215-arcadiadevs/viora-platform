using ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.QueryServices;

/// <summary>
///     Builds the anonymized community-risk snapshot around a reference plot.
/// </summary>
/// <remarks>
///     Signals are derived from the <em>active</em> alerts of neighbor plots within the
///     requested radius. Neighbor identities are intentionally dropped: each signal exposes
///     only the probable threat, severity and approximate distance, so a producer can
///     anticipate nearby threats without learning whose plot raised them.
/// </remarks>
public class CommunityRiskQueryService(
    IExternalAgronomicService externalAgronomicService,
    IAlertRepository alertRepository) : ICommunityRiskQueryService
{
    public async Task<CommunityRiskResource?> Handle(GetCommunityRiskByPlotQuery query, CancellationToken cancellationToken = default)
    {
        var plotName = await externalAgronomicService.GetPlotNameAsync(query.PlotId, cancellationToken);
        if (string.IsNullOrWhiteSpace(plotName))
        {
            return null;
        }

        var neighbors = await externalAgronomicService.FindNeighborPlotsWithinRadiusAsync(
            query.PlotId, query.RadiusKm, cancellationToken);

        if (neighbors.Count == 0)
        {
            return new CommunityRiskResource(
                query.PlotId, plotName, query.RadiusKm, [], PreventiveRecommendations([]));
        }

        var distanceByPlotId = neighbors
            .GroupBy(neighbor => neighbor.PlotId)
            .ToDictionary(group => group.Key, group => group.First().DistanceKm);

        var activeAlerts = await alertRepository.FindActiveByPlotIdInAsync(
            distanceByPlotId.Keys.ToList(), cancellationToken);

        var signals = BuildSignals(activeAlerts, distanceByPlotId);

        return new CommunityRiskResource(
            query.PlotId,
            plotName,
            query.RadiusKm,
            signals,
            PreventiveRecommendations(signals));
    }

    private static List<NearbyRiskSignalResource> BuildSignals(
        IEnumerable<Alert> alerts, IReadOnlyDictionary<long, double> distanceByPlotId)
    {
        var sorted = alerts
            .OrderBy(alert => distanceByPlotId.GetValueOrDefault(alert.PlotId.Value, double.MaxValue))
            .ToList();

        return sorted
            .Select((alert, index) =>
            {
                var threatLabel = ThreatLabel(alert.Type);
                return new NearbyRiskSignalResource(
                    $"signal-{index + 1}",
                    $"Possible {threatLabel} detected nearby",
                    threatLabel,
                    alert.Severity.ToString(),
                    distanceByPlotId.GetValueOrDefault(alert.PlotId.Value, 0.0));
            })
            .ToList();
    }

    /// <summary>Producer-facing label for a threat type used in anonymized signals.</summary>
    private static string ThreatLabel(EThreatType type) => type switch
    {
        EThreatType.XYLELLA_RELATED => "Xylella-related",
        EThreatType.OLIVE_FRUIT_FLY => "olive fruit fly",
        EThreatType.OLIVE_MOTH => "olive moth",
        EThreatType.PEACOCK_SPOT => "peacock spot",
        EThreatType.PHENOLOGICAL_RISK => "phenological imbalance",
        EThreatType.CHILL_DEFICIT or EThreatType.CLIMATE_EXTREME => "climate stress",
        EThreatType.LOW_NDVI => "low-vigor cluster",
        EThreatType.HYDRIC_STRESS or EThreatType.WATER_STRESS => "hydric stress",
        EThreatType.PEST_SYMPTOM or EThreatType.COMMUNITY_PEST => "pest symptom",
        _ => "agronomic threat"
    };

    /// <summary>Derives preventive recommendations from the detected signals.</summary>
    private static List<string> PreventiveRecommendations(IReadOnlyCollection<NearbyRiskSignalResource> signals)
    {
        if (signals.Count == 0)
        {
            return ["No nearby community risk signals detected within the selected radius."];
        }

        return
        [
            "Recommended: inspect leaves and monitor low-vigor zones",
            "Prioritize nearby plots for inspection",
            "Review low-vigor zones in recent vegetation data",
            "Escalate to field inspection if symptoms persist"
        ];
    }
}
