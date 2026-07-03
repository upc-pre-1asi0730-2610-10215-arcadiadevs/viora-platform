using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     Unit tests for <see cref="PhenologicalRiskEvaluator"/>.
///     Template A: pure function, no I/O, no NSubstitute.
///     Tests the risk evaluation matrix: NDVI × chill adequacy → ClimateRiskLevel.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class PhenologicalRiskEvaluatorTests
{
    private static PhenologicalRiskEvaluator BuildSut() => new();

    private static ChillRequirement MakeRequirement(double portions) =>
        new(new ChillPortions(portions), EChillRequirementSource.SystemDefault, EChillMetricModel.Dynamic);

    /// <summary>
    ///     GIVEN NDVI = 0.20 and chill adequacy = 30% (both below Critical thresholds)
    ///     WHEN <see cref="PhenologicalRiskEvaluator.Evaluate"/> is called
    ///     THEN the risk is <see cref="ClimateRiskLevel.Critical"/>.
    /// </summary>
    [Fact]
    public void Evaluate_LowNdviLowChill_ReturnsCritical()
    {
        // GIVEN NDVI well below Critical threshold (0.30) and chill adequacy below 40%
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);

        // WHEN evaluating with NDVI = 0.20 and accumulated = 30 (30% adequacy)
        var result = sut.Evaluate(0.20, requirement, 30m, "Coffee");

        // THEN Critical risk (both NDVI < 0.30 AND chill < 40%)
        Assert.Equal(ClimateRiskLevel.Critical, result);
    }

    /// <summary>
    ///     GIVEN NDVI = 0.70 and chill adequacy = 90% (both above Medium thresholds)
    ///     WHEN <see cref="PhenologicalRiskEvaluator.Evaluate"/> is called
    ///     THEN the risk is <see cref="ClimateRiskLevel.Low"/>.
    /// </summary>
    [Fact]
    public void Evaluate_HighNdviHighChill_ReturnsLow()
    {
        // GIVEN NDVI above Medium threshold (0.60) and chill adequacy above 80%
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);

        // WHEN evaluating with NDVI = 0.70 and accumulated = 90 (90% adequacy)
        var result = sut.Evaluate(0.70, requirement, 90m, "Coffee");

        // THEN Low risk (both NDVI >= 0.60 AND chill >= 80%)
        Assert.Equal(ClimateRiskLevel.Low, result);
    }

    /// <summary>
    ///     GIVEN null NDVI (no data) and zero chill
    ///     WHEN <see cref="PhenologicalRiskEvaluator.Evaluate"/> is called
    ///     THEN the risk is <see cref="ClimateRiskLevel.Critical"/> (0.0 NDVI + 0% chill).
    /// </summary>
    [Fact]
    public void Evaluate_NullNdviZeroChill_ReturnsCritical()
    {
        // GIVEN no NDVI data (treated as 0.0) and zero accumulated chill
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);

        // WHEN evaluating with null NDVI and 0 accumulated
        var result = sut.Evaluate(null, requirement, 0m, null);

        // THEN Critical risk (0.0 < 0.30 AND 0% < 40%)
        Assert.Equal(ClimateRiskLevel.Critical, result);
    }

    /// <summary>
    ///     GIVEN NDVI = 0.50 (between Critical and Medium) and chill adequacy = 90%
    ///     WHEN <see cref="PhenologicalRiskEvaluator.Evaluate"/> is called
    ///     THEN the risk is <see cref="ClimateRiskLevel.Medium"/> (NDVI triggers Medium).
    /// </summary>
    [Fact]
    public void Evaluate_MediumNdviHighChill_ReturnsMedium()
    {
        // GIVEN NDVI between Critical (0.30) and Medium (0.60) — triggers Medium via NDVI
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);

        // WHEN evaluating with NDVI = 0.50 and accumulated = 90 (90% adequacy)
        var result = sut.Evaluate(0.50, requirement, 90m, "Coffee");

        // THEN Medium risk (0.50 < 0.60 triggers Medium, even though chill is fine)
        Assert.Equal(ClimateRiskLevel.Medium, result);
    }

    /// <summary>
    ///     GIVEN zero accumulated chill with a requirement of 1 portion
    ///     WHEN <see cref="PhenologicalRiskEvaluator.Evaluate"/> is called
    ///     THEN chill adequacy = 0% (0/1), so risk is driven by chill alone → High.
    ///     Note: the evaluator only treats zero REQUIREMENT as 100% adequacy;
    ///     zero accumulated with a non-zero requirement yields 0% adequacy.
    /// </summary>
    [Fact]
    public void Evaluate_ZeroAccumulatedChill_ReturnsHigh()
    {
        // GIVEN zero accumulated chill with requirement of 1 portion → 0% adequacy
        var sut = BuildSut();
        var requirement = new ChillRequirement(new ChillPortions(1), EChillRequirementSource.NotConfigured, EChillMetricModel.Dynamic);

        // WHEN evaluating with NDVI = 0.70 and accumulated = 0
        var result = sut.Evaluate(0.70, requirement, 0m, "Coffee");

        // THEN High risk (NDVI 0.70 >= 0.45 so NDVI is fine, but chill 0% < 60% triggers High)
        Assert.Equal(ClimateRiskLevel.High, result);
    }
}
