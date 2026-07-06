using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Coverage for <see cref="SprayVolume"/> — the spray volume for an
///     <see cref="AgrochemicalPrescription"/> (REQ-TP-3). The constructor
///     requires a non-negative amount (zero is allowed, unlike
///     <see cref="Dosage"/>) and a non-blank unit.
/// </summary>
public class SprayVolumeTests
{
    [Theory]
    [InlineData(0, "L")]
    [InlineData(1, "L")]
    [InlineData(500, "mL")]
    public void Constructor_ValidInputs_CreatesSprayVolume(int amount, string unit)
    {
        // GIVEN a non-negative amount and a non-blank unit
        // WHEN the SprayVolume is constructed
        var sprayVolume = new SprayVolume(amount, unit);

        // THEN the properties are set as provided
        Assert.Equal(amount, sprayVolume.Amount);
        Assert.Equal(unit, sprayVolume.Unit);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_NegativeAmount_ThrowsArgumentException(int amount)
    {
        // GIVEN a negative amount
        // WHEN/THEN constructing a SprayVolume throws
        var ex = Assert.Throws<ArgumentException>(() => new SprayVolume(amount, "L"));
        Assert.Equal("amount", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankUnit_ThrowsArgumentException(string? unit)
    {
        // GIVEN a blank unit
        // WHEN/THEN constructing a SprayVolume throws
        var ex = Assert.Throws<ArgumentException>(() => new SprayVolume(10, unit!));
        Assert.Equal("unit", ex.ParamName);
    }
}
