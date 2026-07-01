using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="HydricStressEvaluator"/> (A2 part 1).
///     Hydric stress is reported when the weather snapshot is hot (> 28 °C)
///     AND sunny AND the latest NDVI is below 0.5. The thresholds are
///     hard-coded for v1 (a config-driven version is future work). With no
///     latest statistic the NDVI trend is treated as 0.0 (degraded) so a
///     hot + sunny day with no imagery still triggers when the trend would
///     have been low. A <c>null</c> weather snapshot returns <c>false</c>
///     defensively.
/// </summary>
public class HydricStressEvaluatorTests
{
    private static DateTimeOffset Now { get; } = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    private static WeatherSnapshot Snapshot(decimal temp, WeatherStatus status) =>
        new(temp, status, Now, ClimateRiskLevel.Low);

    private static AgronomicStatistic Stat(double ndvi) =>
        ((Result<AgronomicStatistic, Error>.Success)AgronomicStatistic.Create(
            userId: 1,
            plotId: 1,
            measurementDate: Now,
            ndviValue: ndvi,
            chillPortions: 10.0,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty())).Value;

    private static HydricStressEvaluator BuildEvaluator() =>
        new(Options.Create(new DynamicNutritionPolicyOptions()));

    [Fact]
    public void IsUnderStress_HotSunnyLowNdvi_ReturnsTrue()
    {
        // GIVEN a 30 °C sunny weather snapshot and a latest NDVI of 0.30 (below 0.5)
        var evaluator = BuildEvaluator();

        // WHEN the evaluator runs
        // THEN hydric stress is reported (all 3 conditions met: hot + sunny + low NDVI)
        Assert.True(evaluator.IsUnderStress(Snapshot(30m, WeatherStatus.Sunny), Stat(0.30)));
    }

    [Fact]
    public void IsUnderStress_HotSunnyHighNdvi_ReturnsFalse()
    {
        // GIVEN a 30 °C sunny weather snapshot and a latest NDVI of 0.60 (above 0.5)
        var evaluator = BuildEvaluator();

        // WHEN the evaluator runs
        // THEN hydric stress is NOT reported (the NDVI trend is healthy)
        Assert.False(evaluator.IsUnderStress(Snapshot(30m, WeatherStatus.Sunny), Stat(0.60)));
    }

    [Fact]
    public void IsUnderStress_HotRainyLowNdvi_ReturnsFalse()
    {
        // GIVEN a 30 °C RAINY weather snapshot and a latest NDVI of 0.30
        var evaluator = BuildEvaluator();

        // WHEN the evaluator runs
        // THEN hydric stress is NOT reported (rainy != sunny, so the dry condition is not met)
        Assert.False(evaluator.IsUnderStress(Snapshot(30m, WeatherStatus.Rainy), Stat(0.30)));
    }

    [Fact]
    public void IsUnderStress_ColdSunnyLowNdvi_ReturnsFalse()
    {
        // GIVEN a 20 °C sunny weather snapshot (below the 28 °C threshold) and a latest NDVI of 0.30
        var evaluator = BuildEvaluator();

        // WHEN the evaluator runs
        // THEN hydric stress is NOT reported (not hot, so the temperature condition is not met)
        Assert.False(evaluator.IsUnderStress(Snapshot(20m, WeatherStatus.Sunny), Stat(0.30)));
    }

    [Fact]
    public void IsUnderStress_NullWeather_ReturnsFalse()
    {
        // GIVEN a null weather snapshot (no weather data ingested yet)
        var evaluator = BuildEvaluator();

        // WHEN the evaluator runs
        // THEN hydric stress is NOT reported (defensive: no weather means no trigger)
        Assert.False(evaluator.IsUnderStress(null, Stat(0.30)));
    }

    [Fact]
    public void IsUnderStress_NullLatestNdvi_StillEvaluatesWeatherAsZero()
    {
        // GIVEN a 30 °C sunny weather snapshot and a null latest statistic
        var evaluator = BuildEvaluator();

        // WHEN the evaluator runs
        // THEN hydric stress IS reported (the null trend is treated as the
        // degraded 0.0 floor, which is below the 0.5 threshold; the OS
        // pattern: "missing imagery still surfaces the stress")
        Assert.True(evaluator.IsUnderStress(Snapshot(30m, WeatherStatus.Sunny), null));
    }

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        // GIVEN a null options argument
        // WHEN the evaluator is constructed
        // THEN ArgumentNullException is thrown (defensive ctor)
        Assert.Throws<ArgumentNullException>(() => new HydricStressEvaluator(null!));
    }
}
