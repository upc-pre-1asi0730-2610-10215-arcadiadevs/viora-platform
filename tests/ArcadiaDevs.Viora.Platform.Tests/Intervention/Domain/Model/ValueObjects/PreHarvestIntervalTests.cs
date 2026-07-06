using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Coverage for <see cref="PreHarvestInterval"/> — the pre-harvest
///     safety interval for an <see cref="AgrochemicalPrescription"/>
///     (REQ-TP-3). The constructor requires a non-negative day count (zero
///     is allowed — no waiting period).
/// </summary>
public class PreHarvestIntervalTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(30)]
    public void Constructor_ValidDays_CreatesPreHarvestInterval(int days)
    {
        // GIVEN a non-negative day count
        // WHEN the PreHarvestInterval is constructed
        var interval = new PreHarvestInterval(days);

        // THEN Days is set as provided
        Assert.Equal(days, interval.Days);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    public void Constructor_NegativeDays_ThrowsArgumentException(int days)
    {
        // GIVEN a negative day count
        // WHEN/THEN constructing a PreHarvestInterval throws
        var ex = Assert.Throws<ArgumentException>(() => new PreHarvestInterval(days));
        Assert.Equal("days", ex.ParamName);
    }
}
