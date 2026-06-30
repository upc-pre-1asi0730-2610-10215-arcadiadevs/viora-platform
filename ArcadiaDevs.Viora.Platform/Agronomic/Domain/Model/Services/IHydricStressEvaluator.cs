using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Pure-function evaluator that decides whether a plot is under
///     hydric stress: a hot (> 28 °C) and sunny weather snapshot
///     coinciding with a low (< 0.5) NDVI trend.
///     <para>
///         One of the 3 per-risk evaluators introduced in A2 part 1 (this
///         PR). The simplified implementation matches design §5.2.1: a
///         full trend-aware version (comparing recent NDVI history) is
///         future work. With no latest statistic the trend is treated as
///         0.0 (degraded), so a hot + sunny day with no imagery still
///         triggers a stress alert when the latest observation was low.
///     </para>
/// </summary>
public interface IHydricStressEvaluator
{
    /// <summary>
    ///     Returns <c>true</c> when the weather snapshot is hot and
    ///     sunny AND the latest NDVI is below 0.5, <c>false</c> otherwise
    ///     (a <c>null</c> weather snapshot returns <c>false</c>;
    ///     a <c>null</c> latest statistic still evaluates weather and
    ///     treats the trend as 0.0).
    /// </summary>
    /// <param name="weather">The latest <see cref="WeatherSnapshot"/> for the plot; nullable.</param>
    /// <param name="latest">The latest <see cref="AgronomicStatistic"/>; nullable.</param>
    /// <returns>
    ///     <c>true</c> when a hydric stress is observed; <c>false</c>
    ///     otherwise.
    /// </returns>
    bool IsUnderStress(WeatherSnapshot? weather, AgronomicStatistic? latest);
}
