using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;

/// <summary>
///     Domain service that ranks the full Specialist repository for a given
///     alert (REQ-SPEC-3). Placement as a domain service — not an aggregate
///     method — mirrors the <c>ThreatInferenceService</c> precedent, since
///     ranking operates across the whole repository rather than on a single
///     aggregate instance.
/// </summary>
/// <remarks>
///     Ctor-injected with <see cref="ISpecialistRepository" /> only, no ACL
///     facades (design, obs #267). <c>alertId</c> stays a reserved,
///     currently-unused parameter — same as OS's own
///     <c>SpecialistMatchingPolicy</c>, which documents it as "reserved for
///     future geo/threat-aware ranking".
/// </remarks>
public class SpecialistMatchingPolicy(ISpecialistRepository specialistRepository)
{
    private static readonly IReadOnlyDictionary<EAvailabilityStatus, int> AvailabilityRank =
        new Dictionary<EAvailabilityStatus, int>
        {
            [EAvailabilityStatus.AVAILABLE_TODAY] = 0,
            [EAvailabilityStatus.AVAILABLE_TOMORROW] = 1,
            [EAvailabilityStatus.AVAILABLE_THIS_WEEK] = 2,
            [EAvailabilityStatus.UNAVAILABLE] = 3
        };

    /// <summary>
    ///     Ranks all specialists by availability rank → successRate desc →
    ///     distanceKm asc, capped to <paramref name="limit" />.
    /// </summary>
    /// <param name="alertId">
    ///     Reserved, unused parameter (REQ-SPEC-3) — kept for API/contract
    ///     parity with OS; accepted but not consulted for ranking today.
    /// </param>
    /// <param name="limit">The maximum number of candidates to return (default 3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<Specialist>> MatchSpecialistsForAlertAsync(
        long? alertId,
        int limit = 3,
        CancellationToken cancellationToken = default)
    {
        var all = await specialistRepository.ListAsync(cancellationToken);

        return all
            .OrderBy(s => AvailabilityRank.TryGetValue(s.Availability, out var rank) ? rank : int.MaxValue)
            .ThenByDescending(s => s.SuccessRate)
            .ThenBy(s => s.DistanceKm)
            .Take(limit)
            .ToList()
            .AsReadOnly();
    }
}
