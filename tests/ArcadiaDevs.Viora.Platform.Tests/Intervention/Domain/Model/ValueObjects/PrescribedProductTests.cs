using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Coverage for <see cref="PrescribedProduct"/> — a single product
///     prescribed within an <see cref="AgrochemicalPrescription"/>
///     (REQ-TP-3). Requires a non-blank product name and non-null
///     <see cref="Dosage"/>/<see cref="ApplicationSessions"/>;
///     <c>TechnicalRecommendation</c> defaults to empty when null is passed.
/// </summary>
public class PrescribedProductTests
{
    private static Dosage ValidDosage() => new(5.0, "ml");

    private static ApplicationSessions ValidSessions() => new(3);

    [Fact]
    public void Constructor_ValidInputs_CreatesPrescribedProduct()
    {
        // GIVEN valid inputs for all fields
        var dosage = ValidDosage();
        var sessions = ValidSessions();

        // WHEN the PrescribedProduct is constructed
        var product = new PrescribedProduct("Glyphosate", dosage, sessions, "Apply at dawn");

        // THEN the properties are set as provided
        Assert.Equal("Glyphosate", product.ProductName);
        Assert.Equal(dosage, product.Dosage);
        Assert.Equal(sessions, product.Sessions);
        Assert.Equal("Apply at dawn", product.TechnicalRecommendation);
    }

    [Fact]
    public void Constructor_NullTechnicalRecommendation_DefaultsToEmpty()
    {
        // GIVEN a null technical recommendation
        // WHEN the PrescribedProduct is constructed
        var product = new PrescribedProduct("Glyphosate", ValidDosage(), ValidSessions(), null!);

        // THEN TechnicalRecommendation defaults to empty, not null
        Assert.Equal(string.Empty, product.TechnicalRecommendation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankProductName_ThrowsArgumentException(string? productName)
    {
        // GIVEN a blank product name
        // WHEN/THEN constructing a PrescribedProduct throws
        var ex = Assert.Throws<ArgumentException>(
            () => new PrescribedProduct(productName!, ValidDosage(), ValidSessions(), "note"));
        Assert.Equal("productName", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullDosage_ThrowsArgumentNullException()
    {
        // GIVEN a null Dosage
        // WHEN/THEN constructing a PrescribedProduct throws
        var ex = Assert.Throws<ArgumentNullException>(
            () => new PrescribedProduct("Glyphosate", null!, ValidSessions(), "note"));
        Assert.Equal("dosage", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullSessions_ThrowsArgumentNullException()
    {
        // GIVEN a null ApplicationSessions
        // WHEN/THEN constructing a PrescribedProduct throws
        var ex = Assert.Throws<ArgumentNullException>(
            () => new PrescribedProduct("Glyphosate", ValidDosage(), null!, "note"));
        Assert.Equal("sessions", ex.ParamName);
    }
}
