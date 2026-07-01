using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="ChillDeficitEvaluator"/> (A2 part 1).
///     The evaluator reports a chill deficit when the accumulated portions are
///     strictly below the policy-derived threshold
///     (<c>requirement.Portions x ChillDeficitRatio</c>). The threshold is
///     policy-driven (read from <c>IOptions{DynamicNutritionPolicyOptions}</c>)
///     and a <c>null</c> requirement or accumulated value is treated as
///     "no data, no trigger" (defensive).
/// </summary>
public class ChillDeficitEvaluatorTests
{
    private static ChillRequirement MakeRequirement(double portions) =>
        new(new ChillPortions(portions), EChillRequirementSource.SystemDefault, EChillMetricModel.Dynamic);

    private static IOptions<DynamicNutritionPolicyOptions> OptionsWith(decimal ratio) =>
        Options.Create(new DynamicNutritionPolicyOptions { ChillDeficitRatio = ratio });

    private static IOptions<DynamicNutritionPolicyOptions> OptionsWrapper(DynamicNutritionPolicyOptions options) =>
        Options.Create(options);

    [Fact]
    public void HasDeficit_AccumulatedBelowThreshold_ReturnsTrue()
    {
        // GIVEN a 60-portion requirement and the default 0.7 ratio (threshold = 42)
        var evaluator = new ChillDeficitEvaluator(OptionsWith(0.7m));
        var requirement = MakeRequirement(60.0);

        // WHEN accumulated is 30 (below 42)
        // THEN a deficit is reported
        Assert.True(evaluator.HasDeficit(requirement, 30m));
    }

    [Fact]
    public void HasDeficit_AccumulatedMeetsThreshold_ReturnsFalse()
    {
        // GIVEN a 60-portion requirement and the default 0.7 ratio (threshold = 42)
        var evaluator = new ChillDeficitEvaluator(OptionsWith(0.7m));
        var requirement = MakeRequirement(60.0);

        // WHEN accumulated is 50 (above 42)
        // THEN no deficit is reported
        Assert.False(evaluator.HasDeficit(requirement, 50m));
    }

    [Fact]
    public void HasDeficit_AccumulatedEqualsThreshold_ReturnsFalse()
    {
        // GIVEN a 60-portion requirement and the default 0.7 ratio (threshold = 42)
        var evaluator = new ChillDeficitEvaluator(OptionsWith(0.7m));
        var requirement = MakeRequirement(60.0);

        // WHEN accumulated is exactly 42 (== threshold)
        // THEN no deficit is reported (strict less-than comparison)
        Assert.False(evaluator.HasDeficit(requirement, 42m));
    }

    [Fact]
    public void HasDeficit_NullRequirement_ReturnsFalse()
    {
        // GIVEN a default-policy evaluator
        var evaluator = new ChillDeficitEvaluator(OptionsWith(0.7m));

        // WHEN the requirement is null (defensive)
        // THEN no deficit is reported
        Assert.False(evaluator.HasDeficit(null, 10m));
    }

    [Fact]
    public void HasDeficit_NullAccumulated_ReturnsFalse()
    {
        // GIVEN a default-policy evaluator
        var evaluator = new ChillDeficitEvaluator(OptionsWith(0.7m));
        var requirement = MakeRequirement(60.0);

        // WHEN accumulated is null (no chill data yet)
        // THEN no deficit is reported
        Assert.False(evaluator.HasDeficit(requirement, null));
    }

    [Fact]
    public void HasDeficit_CustomRatio_ShiftsThreshold()
    {
        // GIVEN a 100-portion requirement and a tighter 0.5 ratio (threshold = 50)
        var evaluator = new ChillDeficitEvaluator(OptionsWith(0.5m));
        var requirement = MakeRequirement(100.0);

        // WHEN accumulated is 60 (above 50)
        // THEN no deficit is reported
        Assert.False(evaluator.HasDeficit(requirement, 60m));

        // AND when accumulated is 40 (below 50)
        // THEN a deficit is reported
        Assert.True(evaluator.HasDeficit(requirement, 40m));
    }

    [Fact]
    public void HasDeficit_BoundOptionValueIsNull_UsesDefaultRatio()
    {
        // GIVEN an Options wrapper that returns null for .Value (defensive fallback path)
        var nullValueOptions = Substitute.For<IOptions<DynamicNutritionPolicyOptions>>();
        nullValueOptions.Value.Returns((DynamicNutritionPolicyOptions)null!);
        var evaluator = new ChillDeficitEvaluator(nullValueOptions);
        var requirement = MakeRequirement(60.0);

        // WHEN accumulated is 30 (below the default-ratio threshold of 42)
        // THEN a deficit is reported (the default 0.7 fallback is used)
        Assert.True(evaluator.HasDeficit(requirement, 30m));
    }

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        // GIVEN a null options argument
        // WHEN the evaluator is constructed
        // THEN ArgumentNullException is thrown (defensive ctor)
        Assert.Throws<ArgumentNullException>(() => new ChillDeficitEvaluator(null!));
    }
}
