using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="LowNdviEvaluator"/> (A2 part 1).
///     The evaluator reports a low-NDVI risk when the latest
///     <c>AgronomicStatistic.NdviValue</c> is strictly below the policy's
///     <c>HighRiskNdviThreshold</c>. A <c>null</c> latest statistic returns
///     <c>false</c> (no data means no trigger; the caller surfaces the
///     missing imagery as a separate "no data" case).
/// </summary>
public class LowNdviEvaluatorTests
{
    private static DateTimeOffset Now { get; } = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    private static AgronomicStatistic Stat(double ndvi) =>
        ((Result<AgronomicStatistic, Error>.Success)AgronomicStatistic.Create(
            userId: 1,
            plotId: 1,
            measurementDate: Now,
            ndviValue: ndvi,
            chillPortions: 10.0,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty())).Value;

    private static DynamicNutritionPolicyOptions Policy(decimal highThreshold) =>
        new() { HighRiskNdviThreshold = highThreshold, ModerateRiskNdviThreshold = 0.5m };

    [Fact]
    public void IsBelowThreshold_NdviBelowThreshold_ReturnsTrue()
    {
        // GIVEN the default policy (high threshold 0.30) and a latest NDVI of 0.25
        var evaluator = new LowNdviEvaluator();
        var policy = Policy(0.30m);

        // WHEN the evaluator runs
        // THEN a low-NDVI risk is reported
        Assert.True(evaluator.IsBelowThreshold(Stat(0.25), policy));
    }

    [Fact]
    public void IsBelowThreshold_NdviAboveThreshold_ReturnsFalse()
    {
        // GIVEN the default policy (high threshold 0.30) and a latest NDVI of 0.55
        var evaluator = new LowNdviEvaluator();
        var policy = Policy(0.30m);

        // WHEN the evaluator runs
        // THEN no low-NDVI risk is reported
        Assert.False(evaluator.IsBelowThreshold(Stat(0.55), policy));
    }

    [Fact]
    public void IsBelowThreshold_NdviEqualsThreshold_ReturnsFalse()
    {
        // GIVEN the default policy (high threshold 0.30) and a latest NDVI of exactly 0.30
        var evaluator = new LowNdviEvaluator();
        var policy = Policy(0.30m);

        // WHEN the evaluator runs
        // THEN no low-NDVI risk is reported (strict less-than comparison;
        // matches the OS "NDVI can only RAISE risk" semantics)
        Assert.False(evaluator.IsBelowThreshold(Stat(0.30), policy));
    }

    [Fact]
    public void IsBelowThreshold_NullStatistic_ReturnsFalse()
    {
        // GIVEN a null latest statistic (no imagery ingested yet)
        var evaluator = new LowNdviEvaluator();
        var policy = Policy(0.30m);

        // WHEN the evaluator runs
        // THEN no low-NDVI risk is reported (defensive: no data means no trigger)
        Assert.False(evaluator.IsBelowThreshold(null, policy));
    }

    [Fact]
    public void IsBelowThreshold_CustomThreshold_ShiftsComparison()
    {
        // GIVEN a policy with a higher 0.50 threshold and a latest NDVI of 0.40
        var evaluator = new LowNdviEvaluator();
        var policy = Policy(0.50m);

        // WHEN the evaluator runs
        // THEN a low-NDVI risk is reported (NDVI is now below the tighter threshold)
        Assert.True(evaluator.IsBelowThreshold(Stat(0.40), policy));
    }

    [Fact]
    public void IsBelowThreshold_NullPolicy_Throws()
    {
        // GIVEN a null policy argument
        var evaluator = new LowNdviEvaluator();

        // WHEN the evaluator runs
        // THEN ArgumentNullException is thrown (defensive: policy is required)
        Assert.Throws<ArgumentNullException>(() => evaluator.IsBelowThreshold(Stat(0.25), null!));
    }
}
