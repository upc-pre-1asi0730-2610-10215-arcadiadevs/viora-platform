using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="YieldForecastEstimator"/> (A1, the
///     4th risk evaluator per the user-locked 2026-06-30 decision: chill
///     deficit, low NDVI, hydric stress, yield forecast). The estimator is a
///     pure function with formula
///     <c>5.5 x clamp(0.5 + 0.7 x ndvi, 0.5, 1.2) x min(1, chill / requirement)</c>
///     (rounded to 2 decimals, never negative). A <c>null</c> statistic
///     floors the multiplier to 0.5x and the chill ratio to 0.0 (CC-8: no
///     fabricated data fallback).
/// </summary>
public class YieldForecastEstimatorTests
{
    private static DateTimeOffset Now { get; } = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    private static Plot CreatePlot() =>
        ((Result<Plot, Error>.Success)Plot.Create(
            ownerUserId: 1,
            plotName: "Test plot",
            polygonCoordinates: ((Result<PolygonCoordinates, Error>.Success)PolygonCoordinates.Create(new List<GeoPoint>
            {
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.0m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.1m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.1m, -77.1m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.1m, -77.0m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.0m)).Value,
            })).Value,
            areaSize: 100m,
            cropType: "Coffee",
            variety: "Arabica",
            location: "Lima",
            campaign: "2026",
            notes: "test")).Value;

    private static AgronomicStatistic Stat(double ndvi, double chillPortions) =>
        ((Result<AgronomicStatistic, Error>.Success)AgronomicStatistic.Create(
            userId: 1,
            plotId: 1,
            measurementDate: Now,
            ndviValue: ndvi,
            chillPortions: chillPortions,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty())).Value;

    private static ChillRequirement Requirement(double portions) =>
        new(new ChillPortions(portions), EChillRequirementSource.SystemDefault, EChillMetricModel.Dynamic);

    private static DynamicNutritionPolicyOptions Policy() => new();

    [Fact]
    public void Estimate_FullInputsNdvi1ChillMet_ReturnsBaseYieldTimesCeiling()
    {
        // GIVEN NDVI=1.0 (clamped to 1.2x multiplier) and chillPortions==requirement (chill ratio = 1.0)
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();

        // WHEN the estimator runs
        // THEN the yield is 5.5 x 1.2 x 1.0 = 6.6 (rounded to 2 decimals)
        var yieldTonnes = estimator.Estimate(plot, Stat(1.0, 60.0), Requirement(60.0), Policy());
        Assert.Equal(6.6m, yieldTonnes);
    }

    [Fact]
    public void Estimate_NullStatistic_FloorsToZeroYield()
    {
        // GIVEN a null latest statistic (no imagery data yet)
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();

        // WHEN the estimator runs
        // THEN the yield floors to 0.0 (multiplier 0.5x and chill ratio 0.0;
        // CC-8 no-fabricated-data fallback)
        var yieldTonnes = estimator.Estimate(plot, null, Requirement(60.0), Policy());
        Assert.Equal(0m, yieldTonnes);
    }

    [Fact]
    public void Estimate_NdviAtCeiling_YieldsFullMultiplier()
    {
        // GIVEN NDVI=1.0 (the valid max; the multiplier formula yields 0.5 + 0.7*1.0 = 1.2 exactly)
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();

        // WHEN the estimator runs at NDVI=1.0
        // THEN the yield hits the ceiling 5.5 x 1.2 x 1.0 = 6.6
        var yieldAtCeiling = estimator.Estimate(plot, Stat(1.0, 60.0), Requirement(60.0), Policy());
        Assert.Equal(6.6m, yieldAtCeiling);

        // AND when NDVI is 0.99 (just below the ceiling)
        // THEN the yield is strictly less than the ceiling (no overshoot;
        // 5.5 x 1.193 x 1.0 = 6.56, rounded)
        var yieldBelowCeiling = estimator.Estimate(plot, Stat(0.99, 60.0), Requirement(60.0), Policy());
        Assert.Equal(6.56m, yieldBelowCeiling);
        Assert.True(yieldBelowCeiling < yieldAtCeiling);
    }

    [Fact]
    public void Estimate_LowNdvi_FloorsMultiplier()
    {
        // GIVEN NDVI=0.0 (multiplier floor 0.5x) and chill met
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();

        // WHEN the estimator runs
        // THEN the yield is 5.5 x 0.5 x 1.0 = 2.75
        var yieldTonnes = estimator.Estimate(plot, Stat(0.0, 60.0), Requirement(60.0), Policy());
        Assert.Equal(2.75m, yieldTonnes);
    }

    [Fact]
    public void Estimate_ZeroChillRequirement_TreatsAsFullyAdequate()
    {
        // GIVEN a degenerate zero-portion chill requirement (would be a divide-by-zero
        // in the normal chill-ratio branch)
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();
        var zeroRequirement = new ChillRequirement(new ChillPortions(0.0001), EChillRequirementSource.SystemDefault, EChillMetricModel.Dynamic);

        // WHEN the estimator runs
        // THEN the chill ratio is treated as 1.0 (fully adequate; the zero
        // requirement branch avoids the artificial zero-out)
        var yieldTonnes = estimator.Estimate(plot, Stat(1.0, 60.0), zeroRequirement, Policy());
        Assert.Equal(6.6m, yieldTonnes);
    }

    [Fact]
    public void Estimate_HalfChillPortions_ReturnsHalfYield()
    {
        // GIVEN 30 accumulated portions against a 60-portion requirement (50% chill ratio)
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();

        // WHEN the estimator runs with NDVI=1.0 (multiplier 1.2x) and 50% chill ratio
        // THEN the yield is 5.5 x 1.2 x 0.5 = 3.3
        var yieldTonnes = estimator.Estimate(plot, Stat(1.0, 30.0), Requirement(60.0), Policy());
        Assert.Equal(3.3m, yieldTonnes);
    }

    [Fact]
    public void Estimate_OverChillPortions_ClampsChillRatioToOne()
    {
        // GIVEN 100 accumulated portions against a 60-portion requirement (chill ratio caps at 1.0)
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();

        // WHEN the estimator runs
        // THEN the chill ratio is 1.0 (no overshoot; the requirement is "met" when surpassed)
        var yieldTonnes = estimator.Estimate(plot, Stat(1.0, 100.0), Requirement(60.0), Policy());
        Assert.Equal(6.6m, yieldTonnes);
    }

    [Fact]
    public void Estimate_NullPlot_Throws()
    {
        // GIVEN a null plot argument
        var estimator = new YieldForecastEstimator();

        // WHEN the estimator runs
        // THEN ArgumentNullException is thrown (defensive ctor input)
        Assert.Throws<ArgumentNullException>(() =>
            estimator.Estimate(null!, Stat(0.5, 30.0), Requirement(60.0), Policy()));
    }

    [Fact]
    public void Estimate_NullChillRequirement_Throws()
    {
        // GIVEN a null chill requirement argument
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();

        // WHEN the estimator runs
        // THEN ArgumentNullException is thrown
        Assert.Throws<ArgumentNullException>(() =>
            estimator.Estimate(plot, Stat(0.5, 30.0), null!, Policy()));
    }

    [Fact]
    public void Estimate_NullPolicy_Throws()
    {
        // GIVEN a null policy argument
        var estimator = new YieldForecastEstimator();
        var plot = CreatePlot();

        // WHEN the estimator runs
        // THEN ArgumentNullException is thrown
        Assert.Throws<ArgumentNullException>(() =>
            estimator.Estimate(plot, Stat(0.5, 30.0), Requirement(60.0), null!));
    }
}
