using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Coverage for <see cref="Dosage"/> — the amount/unit of a single
///     <see cref="PrescribedProduct"/> (REQ-TP-3). The constructor requires
///     a strictly positive amount and a non-blank unit.
/// </summary>
public class DosageTests
{
    [Theory]
    [InlineData(0.5, "ml")]
    [InlineData(10, "g")]
    [InlineData(1000.25, "L")]
    public void Constructor_ValidInputs_CreatesDosage(double amount, string unit)
    {
        // GIVEN a positive amount and a non-blank unit
        // WHEN the Dosage is constructed
        var dosage = new Dosage(amount, unit);

        // THEN the properties are set as provided
        Assert.Equal(amount, dosage.Amount);
        Assert.Equal(unit, dosage.Unit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Constructor_NonPositiveAmount_ThrowsArgumentException(double amount)
    {
        // GIVEN a non-positive amount
        // WHEN/THEN constructing a Dosage throws
        var ex = Assert.Throws<ArgumentException>(() => new Dosage(amount, "ml"));
        Assert.Equal("amount", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankUnit_ThrowsArgumentException(string? unit)
    {
        // GIVEN a blank unit
        // WHEN/THEN constructing a Dosage throws
        var ex = Assert.Throws<ArgumentException>(() => new Dosage(5, unit!));
        Assert.Equal("unit", ex.ParamName);
    }
}
