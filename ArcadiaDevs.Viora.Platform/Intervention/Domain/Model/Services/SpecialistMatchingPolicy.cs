using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Acl;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;

/// <summary>
///     A specialist profile ranked for a specific alert-matching pass. Not
///     an aggregate — a transient projection assembled per-call from
///     <see cref="SpecialistProfileSummary" /> plus the computed distance
///     to the alert's plot (null when the alert has no match context, the
///     specialist has no geolocation set, or the distance lookup fails).
/// </summary>
public record RankedSpecialist(SpecialistProfileSummary Profile, double? DistanceKm);

/// <summary>
///     Domain service that ranks real specialist profiles for a given alert
///     (REQ-SPEC-3). Rebuilt to match OS's post-<c>d569bbe</c> design: real
///     geo/tag/availability-aware matching over live <c>Profile</c> data,
///     replacing the previous fixed-catalog ranking over a stored
///     <c>Specialist</c> repository. Placement as a domain service — not an
///     aggregate method — mirrors the <c>ThreatInferenceService</c>
///     precedent, since ranking operates across every specialist profile
///     rather than on a single aggregate instance.
/// </summary>
/// <remarks>
///     Ctor-injected with the three ACL facades this ranking composes:
///     Profile (the specialist directory), Surveillance (the alert's plot +
///     threat type), and Agronomic (plot-centroid distance). No dependency
///     on <see cref="Domain.Repositories.ISpecialistRepository" /> — that
///     repository is now Intervention-local storage (the <c>Whatsapp</c>
///     contact channel only), irrelevant to ranking.
/// </remarks>
public class SpecialistMatchingPolicy(
    IProfileContextFacade profileContextFacade,
    ISurveillanceContextFacade surveillanceContextFacade,
    IAgronomicContextFacade agronomicContextFacade)
{
    /// <summary>
    ///     Default service radius (km) applied when a specialist hasn't set
    ///     their own <c>ServiceRadiusKm</c> — mirrors OS's default.
    /// </summary>
    private const double DefaultServiceRadiusKm = 300;

    private static readonly IReadOnlyDictionary<ESpecialistAvailability, int> AvailabilityRank =
        new Dictionary<ESpecialistAvailability, int>
        {
            [ESpecialistAvailability.AvailableToday] = 0,
            [ESpecialistAvailability.AvailableTomorrow] = 1,
            [ESpecialistAvailability.AvailableThisWeek] = 2,
            [ESpecialistAvailability.Unavailable] = 3
        };

    /// <summary>
    ///     Threat-type -&gt; keyword map for tag-relevance scoring. Hardcoded
    ///     and substring-based (case-insensitive) — mirrors OS's own
    ///     <c>keywordsFor</c>, which is equally hardcoded/brittle; not a
    ///     data-driven config on either side, so this is not something to
    ///     "improve" during the port.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string[]> ThreatKeywords =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["PHENOLOGICAL_RISK"] = new[] { "phenological", "phenology", "crop cycle" },
            ["CHILL_DEFICIT"] = new[] { "chill", "dormancy", "temperature" },
            ["CLIMATE_EXTREME"] = new[] { "climate", "weather", "extreme" },
            ["PEST_SYMPTOM"] = new[] { "pest control", "pest", "symptom", "phytosanitary" },
            ["COMMUNITY_PEST"] = new[] { "pest control", "pest", "integrated pest management", "phytosanitary" },
            ["LOW_NDVI"] = new[] { "ndvi", "vigor", "vegetation", "irrigation" },
            ["HYDRIC_STRESS"] = new[] { "irrigation", "water stress", "hydric" },
            ["XYLELLA_RELATED"] = new[] { "xylella", "biological", "disease", "phytosanitary" },
            ["OLIVE_FRUIT_FLY"] = new[] { "olive fly", "pest control", "integrated pest management" },
            ["OLIVE_MOTH"] = new[] { "olive moth", "pest control", "integrated pest management" },
            ["PEACOCK_SPOT"] = new[] { "peacock spot", "fungal disease", "disease" },
            ["WATER_STRESS"] = new[] { "irrigation", "water stress", "hydric" },
            ["UNKNOWN"] = Array.Empty<string>()
        };

    /// <summary>
    ///     Ranks every specialist Profile for the given alert. Specialists
    ///     within their own <c>ServiceRadiusKm</c> (default
    ///     <see cref="DefaultServiceRadiusKm" /> when unset) of the alert's
    ///     plot are ranked first (relevance desc → availability →
    ///     distance asc), followed by out-of-radius specialists as a
    ///     fallback (distance asc → relevance desc → availability). When
    ///     <paramref name="alertId" /> is null (no alert context — e.g. a
    ///     direct candidate listing with no alert), relevance is 0 for
    ///     everyone and every specialist is treated as in-radius (no
    ///     distance to compare against), so ranking degrades to
    ///     availability-only.
    /// </summary>
    /// <param name="alertId">
    ///     The alert to match against, or null for an alert-agnostic listing.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<RankedSpecialist>> MatchSpecialistsForAlertAsync(
        long? alertId,
        CancellationToken cancellationToken = default)
    {
        var matchContext = alertId.HasValue
            ? await surveillanceContextFacade.GetAlertMatchContextAsync(alertId.Value, cancellationToken)
            : null;

        var profiles = await profileContextFacade.FindSpecialistProfilesAsync(cancellationToken);

        var candidates = new List<(SpecialistProfileSummary Profile, double? DistanceKm, int Relevance, bool InRadius)>();
        foreach (var profile in profiles)
        {
            double? distanceKm = null;
            if (matchContext is not null && profile.Latitude.HasValue && profile.Longitude.HasValue)
            {
                distanceKm = await agronomicContextFacade.DistanceKmFromPlotCentroidAsync(
                    matchContext.PlotId, profile.Latitude.Value, profile.Longitude.Value, cancellationToken);
            }

            var relevance = matchContext is not null ? ComputeRelevance(matchContext.ThreatType, profile.ServiceTags) : 0;

            var radiusKm = profile.ServiceRadiusKm ?? DefaultServiceRadiusKm;
            var inRadius = matchContext is null || distanceKm is null || distanceKm.Value <= radiusKm;

            candidates.Add((profile, distanceKm, relevance, inRadius));
        }

        var inRadiusRanked = candidates
            .Where(c => c.InRadius)
            .OrderByDescending(c => c.Relevance)
            .ThenBy(c => RankOf(c.Profile.Availability))
            .ThenBy(c => c.DistanceKm ?? double.MaxValue)
            .ThenBy(c => c.Profile.UserId);

        var outOfRadiusRanked = candidates
            .Where(c => !c.InRadius)
            .OrderBy(c => c.DistanceKm ?? double.MaxValue)
            .ThenByDescending(c => c.Relevance)
            .ThenBy(c => RankOf(c.Profile.Availability))
            .ThenBy(c => c.Profile.UserId);

        return inRadiusRanked.Concat(outOfRadiusRanked)
            .Select(c => new RankedSpecialist(c.Profile, c.DistanceKm))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    ///     Counts case-insensitive substring keyword matches against the
    ///     specialist's comma-separated <c>ServiceTags</c>. Loosely typed by
    ///     design (matches OS) — not a proper enum/tag taxonomy.
    /// </summary>
    private static int ComputeRelevance(string threatType, string? serviceTags)
    {
        if (string.IsNullOrWhiteSpace(serviceTags) || !ThreatKeywords.TryGetValue(threatType, out var keywords))
        {
            return 0;
        }

        var tags = serviceTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return keywords.Count(keyword => tags.Any(tag => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    ///     Defaults an unset availability to <c>AvailableThisWeek</c>'s rank
    ///     — mirrors OS's own defensive fallback for a null/blank/unknown
    ///     availability value.
    /// </summary>
    private static int RankOf(ESpecialistAvailability? availability)
    {
        return AvailabilityRank.TryGetValue(availability ?? ESpecialistAvailability.AvailableThisWeek, out var rank)
            ? rank
            : AvailabilityRank[ESpecialistAvailability.AvailableThisWeek];
    }
}
