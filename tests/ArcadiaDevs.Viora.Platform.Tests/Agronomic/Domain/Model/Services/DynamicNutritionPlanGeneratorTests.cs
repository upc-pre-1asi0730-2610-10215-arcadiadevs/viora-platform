using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Exceptions;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="DynamicNutritionPlanGenerator"/>
///     (A2 part 2 / PR-D2). Covers the 4-risk trigger matrix (A2 part 1
///     scenarios S2.9..S2.14) plus the empty/null risk CC-7 contract
///     (S2.16). The generator emits 3 NutritionInputRecommendations
///     (foliar Recommended, K-Ca Recommended, biostimulant Optional) on
///     any non-empty risk set. The application window uses
///     <c>ExtremeRiskWindowDays</c> when <see cref="EThreatType.ClimateExtreme"/>
///     is in the set, otherwise <c>HighRiskWindowDays</c>.
/// </summary>
public class DynamicNutritionPlanGeneratorTests
{
    private static DateTimeOffset Now { get; } = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    private static WeatherSnapshot Snapshot(ClimateRiskLevel level) =>
        new(25m, WeatherStatus.Sunny, Now, level);

    private static AgronomicStatistic Stat(double ndvi = 0.4, double chillPortions = 30.0) =>
        ((Result<AgronomicStatistic, Error>.Success)AgronomicStatistic.Create(
            userId: 1,
            plotId: 1,
            measurementDate: Now,
            ndviValue: ndvi,
            chillPortions: chillPortions,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty())).Value;

    private static AgronomicRiskProfile Profile(
        ClimateRiskLevel level = ClimateRiskLevel.Low,
        double ndvi = 0.4,
        AgronomicStatistic? statistic = null) =>
        new(level, new NdviValue(ndvi), Snapshot(level), null, statistic ?? Stat(ndvi));

    private static DynamicNutritionPolicy Policy() =>
        new(
            temperatureReferenceCelsius: 20m,
            highRiskNdviThreshold: 0.30m,
            moderateRiskNdviThreshold: 0.50m,
            highRiskWindowDays: 14,
            extremeRiskWindowDays: 21,
            foliarSupportDosageLitersPerHectare: 2.5m,
            potassiumCalciumDosageKilogramsPerHectare: 3.0m,
            biostimulantDosageLitersPerHectare: 1.2m,
            chillDeficitRatio: 0.7m);

    private static DynamicNutritionPlanGenerator BuildGenerator() => new();

    // ---------- 4-risk matrix ----------

    [Fact]
    public void GeneratePlan_ExtremeClimateRisk_ProducesThreeRecommendations()
    {
        // GIVEN a ClimateExtreme risk
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.ClimateExtreme }, Profile(ClimateRiskLevel.Critical), Policy(), Now);

        // THEN 3 input recommendations are emitted (foliar + K-Ca + biostimulant)
        Assert.Equal(3, plan.InputRecommendations.Count);
        Assert.Equal(ENutritionPlanStatus.Active, plan.Status);
    }

    [Fact]
    public void GeneratePlan_HighClimateRisk_ProducesThreeRecommendations()
    {
        // GIVEN a ClimateHigh risk
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.ClimateHigh }, Profile(ClimateRiskLevel.High), Policy(), Now);

        // THEN 3 input recommendations are emitted
        Assert.Equal(3, plan.InputRecommendations.Count);
    }

    [Fact]
    public void GeneratePlan_ChillDeficitOnly_GeneratesPlan()
    {
        // GIVEN only a ChillDeficit risk (no climate signal)
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.ChillDeficit }, Profile(ClimateRiskLevel.Low), Policy(), Now);

        // THEN 3 recommendations are emitted (the trigger is the risk set, not the climate level alone)
        Assert.Equal(3, plan.InputRecommendations.Count);
        Assert.Contains(EThreatType.ChillDeficit.ToString(), plan.Rationale.Summary);
    }

    [Fact]
    public void GeneratePlan_LowNdviOnly_GeneratesPlan()
    {
        // GIVEN only a LowNdvi risk
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.LowNdvi }, Profile(ClimateRiskLevel.Low), Policy(), Now);

        // THEN 3 recommendations are emitted
        Assert.Equal(3, plan.InputRecommendations.Count);
        Assert.Contains(EThreatType.LowNdvi.ToString(), plan.Rationale.Summary);
    }

    [Fact]
    public void GeneratePlan_HydricStressOnly_GeneratesPlan()
    {
        // GIVEN only a HydricStress risk
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.HydricStress }, Profile(ClimateRiskLevel.Low), Policy(), Now);

        // THEN 3 recommendations are emitted
        Assert.Equal(3, plan.InputRecommendations.Count);
        Assert.Contains(EThreatType.HydricStress.ToString(), plan.Rationale.Summary);
    }

    [Fact]
    public void GeneratePlan_AllFourRisks_GeneratesPlanWithFullRationale()
    {
        // GIVEN all 4 risk types
        var generator = BuildGenerator();
        var risks = new[] { EThreatType.ClimateHigh, EThreatType.ChillDeficit, EThreatType.LowNdvi, EThreatType.HydricStress };

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, risks, Profile(ClimateRiskLevel.High), Policy(), Now);

        // THEN 3 recommendations are emitted and the rationale lists all 4 risk codes
        Assert.Equal(3, plan.InputRecommendations.Count);
        Assert.Contains(EThreatType.ClimateHigh.ToString(), plan.Rationale.Summary);
        Assert.Contains(EThreatType.ChillDeficit.ToString(), plan.Rationale.Summary);
        Assert.Contains(EThreatType.LowNdvi.ToString(), plan.Rationale.Summary);
        Assert.Contains(EThreatType.HydricStress.ToString(), plan.Rationale.Summary);
    }

    // ---------- Application window days ----------

    [Fact]
    public void GeneratePlan_ClimateExtreme_UsesExtremeRiskWindowDays()
    {
        // GIVEN a ClimateExtreme risk
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.ClimateExtreme }, Profile(ClimateRiskLevel.Critical), Policy(), Now);

        // THEN the window is 21 days (ExtremeRiskWindowDays)
        Assert.Equal(Now.AddDays(21), plan.ApplicationWindow.EndDate);
    }

    [Fact]
    public void GeneratePlan_NonExtremeClimate_UsesHighRiskWindowDays()
    {
        // GIVEN a non-extreme risk (ChillDeficit alone)
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.ChillDeficit }, Profile(ClimateRiskLevel.Low), Policy(), Now);

        // THEN the window is 14 days (HighRiskWindowDays; non-extreme risks
        // do not extend the window)
        Assert.Equal(Now.AddDays(14), plan.ApplicationWindow.EndDate);
    }

    [Fact]
    public void GeneratePlan_ClimateHighAmongOtherRisks_StillUsesHighWindow()
    {
        // GIVEN ClimateHigh (not ClimateExtreme) plus other risks
        var generator = BuildGenerator();
        var risks = new[] { EThreatType.ClimateHigh, EThreatType.ChillDeficit };

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, risks, Profile(ClimateRiskLevel.High), Policy(), Now);

        // THEN the window is 14 days (HighRiskWindowDays; only ClimateExtreme extends to 21)
        Assert.Equal(Now.AddDays(14), plan.ApplicationWindow.EndDate);
    }

    // ---------- Recommendation contract ----------

    [Fact]
    public void GeneratePlan_FoliarAndKCaAreRecommended_BiostimulantIsOptional()
    {
        // GIVEN any triggering risk
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.LowNdvi }, Profile(), Policy(), Now);

        // THEN the foliar and K-Ca recommendations are Recommended and the biostimulant is Optional
        var foliar = Assert.Single(plan.InputRecommendations, r => r.Value == "Foliar nutrition support");
        var potassiumCalcium = Assert.Single(plan.InputRecommendations, r => r.Value == "Potassium-calcium support");
        var biostimulant = Assert.Single(plan.InputRecommendations, r => r.Value == "Biostimulant support");

        Assert.Equal(ENutritionInputStatus.Recommended, foliar.Status);
        Assert.Equal(ENutritionInputStatus.Recommended, potassiumCalcium.Status);
        Assert.Equal(ENutritionInputStatus.Optional, biostimulant.Status);

        // AND the dosages come straight from the policy
        Assert.Equal(2.5, foliar.Dosage);
        Assert.Equal(3.0, potassiumCalcium.Dosage);
        Assert.Equal(1.2, biostimulant.Dosage);
    }

    [Fact]
    public void GeneratePlan_ExtremeClimate_MapsClimateRiskLevelToExtreme()
    {
        // GIVEN a ClimateExtreme risk with a Critical snapshot
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.ClimateExtreme }, Profile(ClimateRiskLevel.Critical), Policy(), Now);

        // THEN the rationale's triggering level is the OS-parity "Extreme"
        // (Critical on the snapshot maps to Extreme on the rationale VO)
        Assert.Equal(EClimateRiskLevel.Extreme, plan.Rationale.TriggeringRiskLevel);
    }

    [Fact]
    public void GeneratePlan_MediumClimate_MapsClimateRiskLevelToModerate()
    {
        // GIVEN a Medium climate snapshot (no climate risk in the risk set;
        // the chill deficit carries the load)
        var generator = BuildGenerator();

        // WHEN the generator runs
        var plan = generator.GeneratePlan(1, 1, new[] { EThreatType.ChillDeficit }, Profile(ClimateRiskLevel.Medium), Policy(), Now);

        // THEN the rationale's triggering level is the OS-parity "Moderate"
        // (Medium on the snapshot maps to Moderate on the rationale VO)
        Assert.Equal(EClimateRiskLevel.Moderate, plan.Rationale.TriggeringRiskLevel);
    }

    // ---------- CC-7 empty/null contract ----------

    [Fact]
    public void GeneratePlan_EmptyRisks_ThrowsUnavailableException()
    {
        // GIVEN an empty risk collection
        var generator = BuildGenerator();

        // WHEN the generator runs
        // THEN DynamicNutritionPlanUnavailableException is thrown (CC-7: no silent default)
        Assert.Throws<DynamicNutritionPlanUnavailableException>(() =>
            generator.GeneratePlan(1, 1, Array.Empty<EThreatType>(), Profile(), Policy(), Now));
    }

    [Fact]
    public void GeneratePlan_NullRisks_ThrowsArgumentNullException()
    {
        // GIVEN a null risk collection
        var generator = BuildGenerator();

        // WHEN the generator runs
        // THEN ArgumentNullException is thrown (defensive: null is not an empty risk set)
        Assert.Throws<ArgumentNullException>(() =>
            generator.GeneratePlan(1, 1, null!, Profile(), Policy(), Now));
    }

    [Fact]
    public void GeneratePlan_NullProfile_ThrowsArgumentNullException()
    {
        // GIVEN a null AgronomicRiskProfile
        var generator = BuildGenerator();

        // WHEN the generator runs
        // THEN ArgumentNullException is thrown
        Assert.Throws<ArgumentNullException>(() =>
            generator.GeneratePlan(1, 1, new[] { EThreatType.LowNdvi }, null!, Policy(), Now));
    }

    [Fact]
    public void GeneratePlan_NullPolicy_ThrowsArgumentNullException()
    {
        // GIVEN a null DynamicNutritionPolicy
        var generator = BuildGenerator();

        // WHEN the generator runs
        // THEN ArgumentNullException is thrown
        Assert.Throws<ArgumentNullException>(() =>
            generator.GeneratePlan(1, 1, new[] { EThreatType.LowNdvi }, Profile(), null!, Now));
    }
}
