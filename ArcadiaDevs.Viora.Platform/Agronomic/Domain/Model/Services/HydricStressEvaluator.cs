using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Pure-function implementation of <see cref="IHydricStressEvaluator"/>.
///     <para>
///         Following design §5.2.1: hydric stress is reported when the
///         weather snapshot is hot (> 28 °C) AND sunny AND the latest
///         NDVI is below 0.5. The 28 °C and 0.5 thresholds are
///         hard-coded for v1 (a config-driven version is future work);
///         the <c>DynamicNutritionPolicyOptions</c> dependency is
///         injected for consistency with the other 2 evaluators so
///         all 3 evaluators are registered against the same options
///         instance in DI.
///     </para>
///     <para>
///         With no latest statistic the NDVI trend is treated as 0.0
///         (degraded) so a hot + sunny day with no imagery still
///         triggers a stress alert when the latest observation was
///         low. A <c>null</c> weather snapshot returns <c>false</c>
///         defensively (no weather data means no trigger).
///     </para>
/// </summary>
public sealed class HydricStressEvaluator : IHydricStressEvaluator
{
    /// <summary>
    ///     Hard-coded hot-weather threshold (°C) per design §5.2.1.
    /// </summary>
    private const decimal HotWeatherThresholdCelsius = 28m;

    /// <summary>
    ///     Hard-coded low-NDVI threshold per design §5.2.1.
    /// </summary>
    private const decimal LowNdviTrendThreshold = 0.5m;

    private readonly IOptions<DynamicNutritionPolicyOptions> _options;

    /// <summary>
    ///     Builds a new <see cref="HydricStressEvaluator"/>.
    /// </summary>
    /// <param name="options">
    ///     The bound options. Not consumed in the v1 hard-coded
    ///     threshold logic, but carried in the constructor so the
    ///     evaluator participates in the same DI wiring as the
    ///     <see cref="ChillDeficitEvaluator"/> and
    ///     <see cref="LowNdviEvaluator"/>, and so a future change
    ///     can move the thresholds to config without touching the
    ///     DI graph.
    /// </param>
    public HydricStressEvaluator(IOptions<DynamicNutritionPolicyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public bool IsUnderStress(WeatherSnapshot? weather, AgronomicStatistic? latest)
    {
        if (weather is null)
        {
            return false;
        }

        var hot = weather.CurrentTemperature > HotWeatherThresholdCelsius;
        var dry = weather.WeatherStatus == WeatherStatus.Sunny;

        // AgronomicStatistic.NdviValue is double; a null latest yields
        // the degraded trend (0.0) so the condition can still fire.
        var trend = latest is null ? 0.0m : (decimal)latest.NdviValue;

        return hot && dry && trend < LowNdviTrendThreshold;
    }
}
