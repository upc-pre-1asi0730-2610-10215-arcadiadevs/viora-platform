using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="ChillAccumulationCalculator"/>
///     (A2 part 1). The calculator accumulates winter chill from a window
///     of hourly weather using the Dynamic Model (Fishman &amp; Erez 1987;
///     Luedeling et al. 2009 / chillR). A "chilling hours" sub-counter is
///     incremented for each reading between 0 °C and 7.2 °C (inclusive).
///     The carry-over <see cref="ChillModelState"/> lets the next
///     accumulation continue from where the previous one stopped.
/// </summary>
public class ChillAccumulationCalculatorTests
{
    private static DateTimeOffset Now { get; } = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

    private static WeatherReading Reading(double tempC, int hourOffset, WeatherStatus status = WeatherStatus.Sunny) =>
        new(Now.AddHours(hourOffset), tempC, status);

    [Fact]
    public void Accumulate_NullHistory_ThrowsArgumentNullException()
    {
        // GIVEN a null history
        var calculator = new ChillAccumulationCalculator();

        // WHEN the calculator runs
        // THEN ArgumentNullException is thrown (defensive: history is required)
        Assert.Throws<ArgumentNullException>(() =>
            calculator.Accumulate(null!, ChillModelState.Empty()));
    }

    [Fact]
    public void Accumulate_SingleReadingInChillingHoursRange_RecordsOneHour()
    {
        // GIVEN a single 5 °C reading (within the 0..7.2 °C chilling hours range)
        var calculator = new ChillAccumulationCalculator();
        var history = new WeatherHistory(new[] { Reading(5.0, 0) });

        // WHEN the calculator runs
        var result = calculator.Accumulate(history, ChillModelState.Empty());

        // THEN exactly 1 chilling hour is recorded
        Assert.Equal(1.0, result.ChillHours);
    }

    [Fact]
    public void Accumulate_HotReadings_RecordZeroChillingHours()
    {
        // GIVEN 3 readings all above 7.2 °C (the upper bound of the chilling hours range)
        var calculator = new ChillAccumulationCalculator();
        var history = new WeatherHistory(new[]
        {
            Reading(20.0, 0),
            Reading(25.0, 1),
            Reading(30.0, 2)
        });

        // WHEN the calculator runs
        var result = calculator.Accumulate(history, ChillModelState.Empty());

        // THEN 0 chilling hours are recorded
        Assert.Equal(0.0, result.ChillHours);
    }

    [Fact]
    public void Accumulate_ColdReadings_RecordExpectedChillingHours()
    {
        // GIVEN 5 readings: 3 in range (5 °C, 3 °C, 7 °C) and 2 out of range (20 °C, -1 °C)
        var calculator = new ChillAccumulationCalculator();
        var history = new WeatherHistory(new[]
        {
            Reading(5.0, 0),
            Reading(20.0, 1),
            Reading(3.0, 2),
            Reading(-1.0, 3),
            Reading(7.0, 4)
        });

        // WHEN the calculator runs
        var result = calculator.Accumulate(history, ChillModelState.Empty());

        // THEN exactly 3 chilling hours are recorded (the 3 in-range readings)
        Assert.Equal(3.0, result.ChillHours);
    }

    [Fact]
    public void Accumulate_ReturnsCarryOverStateForNextWindow()
    {
        // GIVEN a 3-reading history
        var calculator = new ChillAccumulationCalculator();
        var history = new WeatherHistory(new[]
        {
            Reading(5.0, 0),
            Reading(6.0, 1),
            Reading(4.0, 2)
        });

        // WHEN the calculator runs
        var result = calculator.Accumulate(history, ChillModelState.Empty());

        // THEN a non-null carry-over state is returned (the next call can continue from there)
        Assert.NotNull(result.NewState);
    }

    [Fact]
    public void Accumulate_BoundaryTemperatures_AreInclusive()
    {
        // GIVEN 2 readings at the exact boundaries (0 °C and 7.2 °C)
        var calculator = new ChillAccumulationCalculator();
        var history = new WeatherHistory(new[]
        {
            Reading(0.0, 0),
            Reading(7.2, 1)
        });

        // WHEN the calculator runs
        var result = calculator.Accumulate(history, ChillModelState.Empty());

        // THEN both readings count toward the chilling hours (inclusive boundaries)
        Assert.Equal(2.0, result.ChillHours);
    }

    [Fact]
    public void WeatherHistory_Empty_Throws()
    {
        // GIVEN an empty reading list
        // WHEN the WeatherHistory VO is constructed
        // THEN ArgumentException is thrown (defensive: history must contain at least one reading)
        Assert.Throws<ArgumentException>(() => new WeatherHistory(Array.Empty<WeatherReading>()));
    }
}
