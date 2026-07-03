using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     Unit tests for <see cref="ChillSeasonEvaluator"/>.
///     Template A: pure function, no I/O, no NSubstitute.
///     Tests the chill season state evaluation: accumulated chill × requirement → ChillSeasonState.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class ChillSeasonEvaluatorTests
{
    private static ChillSeasonEvaluator BuildSut() => new();

    private static ChillRequirement MakeRequirement(double portions) =>
        new(new ChillPortions(portions), EChillRequirementSource.SystemDefault, EChillMetricModel.Dynamic);

    /// <summary>
    ///     GIVEN accumulated chill = 80, requirement = 100, and current month = January (in season)
    ///     WHEN <see cref="ChillSeasonEvaluator.Evaluate"/> is called
    ///     THEN progress = 0.8 and status is <see cref="ChillSeasonStatus.OnTrack"/>
    ///     (in season + progress >= 0.5).
    /// </summary>
    [Fact]
    public void Evaluate_InSeasonHalfAccumulated_ReturnsOnTrack()
    {
        // GIVEN 80 accumulated chill out of 100 required, January (in chill season)
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);
        var now = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        // WHEN evaluating
        var result = sut.Evaluate(80m, requirement, now);

        // THEN progress = 0.8, status = OnTrack (in season + progress >= 0.5)
        Assert.Equal(0.8, result.ProgressRatio);
        Assert.Equal(ChillSeasonStatus.OnTrack, result.Status);
        Assert.Equal(80m, result.AccumulatedPortions);
    }

    /// <summary>
    ///     GIVEN accumulated chill = 0, requirement = 100, and current month = July (not in season)
    ///     WHEN <see cref="ChillSeasonEvaluator.Evaluate"/> is called
    ///     THEN progress = 0.0 and status is <see cref="ChillSeasonStatus.NotStarted"/>.
    /// </summary>
    [Fact]
    public void Evaluate_NotStarted_ReturnsNotStarted()
    {
        // GIVEN 0 accumulated chill, July (outside chill season Oct–Feb)
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);
        var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

        // WHEN evaluating
        var result = sut.Evaluate(0m, requirement, now);

        // THEN progress = 0.0, status = NotStarted
        Assert.Equal(0.0, result.ProgressRatio);
        Assert.Equal(ChillSeasonStatus.NotStarted, result.Status);
    }

    /// <summary>
    ///     GIVEN accumulated chill = 100, requirement = 100
    ///     WHEN <see cref="ChillSeasonEvaluator.Evaluate"/> is called
    ///     THEN progress = 1.0 and status is <see cref="ChillSeasonStatus.RequirementMet"/>.
    /// </summary>
    [Fact]
    public void Evaluate_RequirementMet_ReturnsRequirementMet()
    {
        // GIVEN fully accumulated chill
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);
        var now = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        // WHEN evaluating
        var result = sut.Evaluate(100m, requirement, now);

        // THEN progress = 1.0 (clamped), status = RequirementMet
        Assert.Equal(1.0, result.ProgressRatio);
        Assert.Equal(ChillSeasonStatus.RequirementMet, result.Status);
    }

    /// <summary>
    ///     GIVEN accumulated chill = 150, requirement = 100 (exceeds requirement)
    ///     WHEN <see cref="ChillSeasonEvaluator.Evaluate"/> is called
    ///     THEN progress is clamped to 1.0 and status is <see cref="ChillSeasonStatus.RequirementMet"/>.
    /// </summary>
    [Fact]
    public void Evaluate_ExceedsRequirement_ClampsToOne()
    {
        // GIVEN accumulated exceeds requirement
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);
        var now = new DateTimeOffset(2026, 2, 15, 12, 0, 0, TimeSpan.Zero);

        // WHEN evaluating
        var result = sut.Evaluate(150m, requirement, now);

        // THEN progress clamped to 1.0
        Assert.Equal(1.0, result.ProgressRatio);
        Assert.Equal(ChillSeasonStatus.RequirementMet, result.Status);
    }

    /// <summary>
    ///     GIVEN accumulated chill = 30, requirement = 100, and current month = August (off season)
    ///     WHEN <see cref="ChillSeasonEvaluator.Evaluate"/> is called
    ///     THEN status is <see cref="ChillSeasonStatus.OffSeason"/> (progress > 0 but not in season).
    /// </summary>
    [Fact]
    public void Evaluate_OffSeasonPartialAccumulation_ReturnsOffSeason()
    {
        // GIVEN partial accumulation in August (outside Oct–Feb)
        var sut = BuildSut();
        var requirement = MakeRequirement(100.0);
        var now = new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

        // WHEN evaluating
        var result = sut.Evaluate(30m, requirement, now);

        // THEN progress = 0.3, status = OffSeason
        Assert.Equal(0.3, result.ProgressRatio);
        Assert.Equal(ChillSeasonStatus.OffSeason, result.Status);
    }
}
