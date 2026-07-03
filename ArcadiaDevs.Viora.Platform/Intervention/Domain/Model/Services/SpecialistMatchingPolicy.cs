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
    ///     distanceKm asc → id asc (deterministic final tie-breaker).
    /// </summary>
    /// <remarks>
    ///     Returns the FULL ranked repository, uncapped — the underlying
    ///     <c>ISpecialistRepository.ListAsync</c> issues no
    ///     <c>ORDER BY</c>, so the id tie-breaker also guarantees a
    ///     deterministic result across calls. Capping to a caller-requested
    ///     limit is intentionally NOT done here; it is the caller's
    ///     responsibility (see <c>SpecialistQueryService</c>), since the
    ///     caller may need to filter ranked results (e.g. by
    ///     profile-resolvability) before trimming to the requested size —
    ///     capping here first could silently under-fill the final result.
    /// </remarks>
    /// <param name="alertId">
    ///     Reserved, unused parameter (REQ-SPEC-3) — kept for API/contract
    ///     parity with OS; accepted but not consulted for ranking today.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<Specialist>> MatchSpecialistsForAlertAsync(
        long? alertId,
        CancellationToken cancellationToken = default)
    {
        var all = await specialistRepository.ListAsync(cancellationToken);

        return all
            .OrderBy(s => AvailabilityRank.TryGetValue(s.Availability, out var rank) ? rank : int.MaxValue)
            .ThenByDescending(s => s.SuccessRate)
            .ThenBy(s => s.DistanceKm)
            .ThenBy(s => s.Id)
            .ToList()
            .AsReadOnly();
    }
}
