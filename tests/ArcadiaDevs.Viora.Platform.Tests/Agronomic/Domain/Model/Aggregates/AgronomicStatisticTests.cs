using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Aggregates;

/// <summary>
///     AGRO-002 hardening tests for the <see cref="AgronomicStatistic"/> aggregate.
///     The aggregate must validate inputs through a <c>Create</c> factory
///     returning <see cref="Result{TValue, TError}"/>, and expose
///     <c>RecordReading</c> for in-place updates under the same validation
///     contract.
/// </summary>
public class AgronomicStatisticTests
{
    private static DateTimeOffset Today { get; } =
        new(2026, 6, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_InvalidInputs_ReturnsFailure()
    {
        // Arrange — userId=0, plotId=0, negative chill, out-of-range NDVI.
        // One or more invariants must be violated to drive the failure.

        // Act — non-positive userId
        var resultNonPositiveUser = AgronomicStatistic.Create(
            userId: 0,
            plotId: 1,
            measurementDate: Today,
            ndviValue: 0.4,
            chillPortions: 10.0,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty());

        // Assert
        Assert.True(resultNonPositiveUser.IsFailure);
        var error = ((Result<AgronomicStatistic, Error>.Failure)resultNonPositiveUser).Error;
        Assert.Equal("USER_ID_REQUIRED", error.Code);
    }

    [Fact]
    public void Create_WithNonPositivePlotId_ReturnsFailure()
    {
        var result = AgronomicStatistic.Create(
            userId: 1,
            plotId: 0,
            measurementDate: Today,
            ndviValue: 0.4,
            chillPortions: 10.0,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty());

        Assert.True(result.IsFailure);
        var error = ((Result<AgronomicStatistic, Error>.Failure)result).Error;
        Assert.Equal("PLOT_ID_REQUIRED", error.Code);
    }

    [Fact]
    public void Create_WithOutOfRangeNdvi_ReturnsFailure()
    {
        var result = AgronomicStatistic.Create(
            userId: 1,
            plotId: 1,
            measurementDate: Today,
            ndviValue: 2.5, // NDVI is bounded in [-1, 1]
            chillPortions: 10.0,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty());

        Assert.True(result.IsFailure);
        var error = ((Result<AgronomicStatistic, Error>.Failure)result).Error;
        Assert.Equal("NDVI_OUT_OF_RANGE", error.Code);
    }

    [Fact]
    public void Create_WithNegativeChillPortions_ReturnsFailure()
    {
        var result = AgronomicStatistic.Create(
            userId: 1,
            plotId: 1,
            measurementDate: Today,
            ndviValue: 0.4,
            chillPortions: -1.0,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty());

        Assert.True(result.IsFailure);
        var error = ((Result<AgronomicStatistic, Error>.Failure)result).Error;
        Assert.Equal("CHILL_PORTIONS_NEGATIVE", error.Code);
    }

    [Fact]
    public void Create_WithNegativeChillHours_ReturnsFailure()
    {
        var result = AgronomicStatistic.Create(
            userId: 1,
            plotId: 1,
            measurementDate: Today,
            ndviValue: 0.4,
            chillPortions: 10.0,
            chillHours: -0.5,
            chillModelState: ChillModelState.Empty());

        Assert.True(result.IsFailure);
        var error = ((Result<AgronomicStatistic, Error>.Failure)result).Error;
        Assert.Equal("CHILL_HOURS_NEGATIVE", error.Code);
    }

    [Fact]
    public void Create_WithValidInputs_ReturnsSuccess_AndPersistsValues()
    {
        var state = new ChillModelState(0.5, 12.0, 11.5);

        var result = AgronomicStatistic.Create(
            userId: 7,
            plotId: 3,
            measurementDate: Today,
            ndviValue: 0.42,
            chillPortions: 25.5,
            chillHours: 100.0,
            chillModelState: state);

        Assert.True(result.IsSuccess);
        var statistic = ((Result<AgronomicStatistic, Error>.Success)result).Value;
        Assert.Equal(7L, statistic.UserId);
        Assert.Equal(3L, statistic.PlotId);
        Assert.Equal(Today, statistic.MeasurementDate);
        Assert.Equal(0.42, statistic.NdviValue);
        Assert.Equal(25.5, statistic.ChillPortions);
        Assert.Equal(100.0, statistic.ChillHours);
        Assert.Equal(state, statistic.ChillModelState);
    }

    [Fact]
    public void Create_WithNullChillModelState_DefaultsToEmpty()
    {
        var result = AgronomicStatistic.Create(
            userId: 1,
            plotId: 1,
            measurementDate: Today,
            ndviValue: 0.0,
            chillPortions: 0.0,
            chillHours: 0.0,
            chillModelState: null!);

        Assert.True(result.IsSuccess);
        var statistic = ((Result<AgronomicStatistic, Error>.Success)result).Value;
        Assert.Equal(ChillModelState.Empty(), statistic.ChillModelState);
    }

    [Fact]
    public void RecordReading_WithInvalidInputs_ReturnsFailure_AndDoesNotMutate()
    {
        var initial = ((Result<AgronomicStatistic, Error>.Success)AgronomicStatistic.Create(
            userId: 1, plotId: 1, measurementDate: Today,
            ndviValue: 0.4, chillPortions: 10.0, chillHours: 20.0,
            chillModelState: ChillModelState.Empty())).Value;

        var result = initial.RecordReading(
            ndviValue: 5.0, // out of range
            chillPortions: 10.0,
            chillHours: 20.0,
            chillModelState: ChillModelState.Empty());

        Assert.True(result.IsFailure);
        // State must be unchanged
        Assert.Equal(0.4, initial.NdviValue);
        Assert.Equal(10.0, initial.ChillPortions);
        Assert.Equal(20.0, initial.ChillHours);
    }

    [Fact]
    public void RecordReading_WithValidInputs_UpdatesValues()
    {
        var statistic = ((Result<AgronomicStatistic, Error>.Success)AgronomicStatistic.Create(
            userId: 1, plotId: 1, measurementDate: Today,
            ndviValue: 0.4, chillPortions: 10.0, chillHours: 20.0,
            chillModelState: ChillModelState.Empty())).Value;

        var newState = new ChillModelState(1.0, 13.0, 12.0);
        var result = statistic.RecordReading(
            ndviValue: 0.55,
            chillPortions: 35.0,
            chillHours: 80.0,
            chillModelState: newState);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.55, statistic.NdviValue);
        Assert.Equal(35.0, statistic.ChillPortions);
        Assert.Equal(80.0, statistic.ChillHours);
        Assert.Equal(newState, statistic.ChillModelState);
    }
}
