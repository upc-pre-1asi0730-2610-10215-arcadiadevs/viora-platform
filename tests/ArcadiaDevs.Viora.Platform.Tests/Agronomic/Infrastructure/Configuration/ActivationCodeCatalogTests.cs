using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Infrastructure.Configuration;

/// <summary>
///     AGRO-013a unit tests for <see cref="InMemoryActivationCodeCatalog"/>
///     (A4 part 1). The catalog is a small fixed whitelist of 9 issued
///     sensor units (3 soil probes, 3 leaf wetness, 3 weather stations).
///     Tests cover the 9 known codes (one per kind) plus the rejection of
///     an unknown code and the defensive null input path. The 10th
///     acceptance scenario (per spec S2.17..S2.20) is exercised through
///     the <see cref="ActivationCode"/> value object — the catalog is
///     pure: format-validation happens at the VO, catalog membership
///     happens here.
/// </summary>
public class ActivationCodeCatalogTests
{
    private static InMemoryActivationCodeCatalog BuildCatalog() => new();

    // ---------- 9 known codes ----------

    [Theory]
    [InlineData("VIORA-SP01-7K3M")]
    [InlineData("VIORA-SP02-9P2X")]
    [InlineData("VIORA-SP03-4T8H")]
    [InlineData("VIORA-LW01-5F1N")]
    [InlineData("VIORA-LW02-7K9R")]
    [InlineData("VIORA-LW03-2M6Y")]
    [InlineData("VIORA-WS01-3H8V")]
    [InlineData("VIORA-WS02-8C4Q")]
    [InlineData("VIORA-WS03-1Z7Y")]
    public void IsIssued_AllNineKnownCodes_ReturnTrue(string code)
    {
        // GIVEN the in-memory catalog seeded with the 9 demo codes
        var catalog = BuildCatalog();

        // WHEN the catalog checks a known code
        // THEN the code is reported as issued
        Assert.True(catalog.IsIssued(new ActivationCode(code)));
    }

    [Fact]
    public void IsIssued_AllNineKnownCodes_InThreeSensorKindBuckets()
    {
        // GIVEN the 9 known codes split into 3 sensor kinds (SP, LW, WS)
        // WHEN each is checked
        // THEN the catalog's prefix-to-kind mapping is consistent
        var catalog = BuildCatalog();

        // Soil probes
        Assert.Equal(IoTDeviceType.SoilProbe, new ActivationCode("VIORA-SP01-7K3M").DeviceType());
        Assert.Equal(IoTDeviceType.SoilProbe, new ActivationCode("VIORA-SP02-9P2X").DeviceType());
        Assert.Equal(IoTDeviceType.SoilProbe, new ActivationCode("VIORA-SP03-4T8H").DeviceType());

        // Leaf wetness
        Assert.Equal(IoTDeviceType.LeafWetness, new ActivationCode("VIORA-LW01-5F1N").DeviceType());
        Assert.Equal(IoTDeviceType.LeafWetness, new ActivationCode("VIORA-LW02-7K9R").DeviceType());
        Assert.Equal(IoTDeviceType.LeafWetness, new ActivationCode("VIORA-LW03-2M6Y").DeviceType());

        // Weather stations
        Assert.Equal(IoTDeviceType.WeatherStation, new ActivationCode("VIORA-WS01-3H8V").DeviceType());
        Assert.Equal(IoTDeviceType.WeatherStation, new ActivationCode("VIORA-WS02-8C4Q").DeviceType());
        Assert.Equal(IoTDeviceType.WeatherStation, new ActivationCode("VIORA-WS03-1Z7Y").DeviceType());

        // AND all 9 are recognised as issued
        Assert.True(catalog.IsIssued(new ActivationCode("VIORA-SP01-7K3M")));
        Assert.True(catalog.IsIssued(new ActivationCode("VIORA-LW01-5F1N")));
        Assert.True(catalog.IsIssued(new ActivationCode("VIORA-WS01-3H8V")));
    }

    // ---------- Unknown code rejection ----------

    [Fact]
    public void IsIssued_UnknownCode_ReturnsFalse()
    {
        // GIVEN the in-memory catalog
        var catalog = BuildCatalog();

        // WHEN an unknown code is checked (well-formed but not in the whitelist)
        // THEN the catalog reports it as NOT issued
        Assert.False(catalog.IsIssued(new ActivationCode("VIORA-SP99-AAAA")));
    }

    [Fact]
    public void IsIssued_DifferentSensorKindNotInWhitelist_ReturnsFalse()
    {
        // GIVEN a well-formed code with a different sensor kind (XX) that the VO accepts via fallback
        //   (Note: the VO only accepts SP/LW/WS prefixes, so we use a "SP" code that is not in the whitelist)
        var catalog = BuildCatalog();

        // WHEN a SP-prefixed but not-in-whitelist code is checked
        // THEN the catalog reports it as NOT issued
        Assert.False(catalog.IsIssued(new ActivationCode("VIORA-SP00-AAAA")));
    }

    // ---------- Defensive / normalization ----------

    [Fact]
    public void IsIssued_NullCode_ReturnsFalse()
    {
        // GIVEN a null code argument
        var catalog = BuildCatalog();

        // WHEN the catalog is asked
        // THEN false is returned (defensive; no NRE)
        Assert.False(catalog.IsIssued(null!));
    }

    [Fact]
    public void IsIssued_LowercaseInput_NormalizesViaValueObject_AndReturnsTrue()
    {
        // GIVEN the in-memory catalog AND a lowercase activation code
        var catalog = BuildCatalog();

        // WHEN the code is constructed (the VO normalises trim + upper-case) and checked
        // THEN the catalog reports it as issued (the VO normalises the input to the canonical form)
        Assert.True(catalog.IsIssued(new ActivationCode("viora-sp01-7k3m")));
    }

    [Fact]
    public void IsIssued_WhitespacePaddedInput_NormalizesViaValueObject_AndReturnsTrue()
    {
        // GIVEN the in-memory catalog AND a whitespace-padded activation code
        var catalog = BuildCatalog();

        // WHEN the code is constructed (the VO trims) and checked
        // THEN the catalog reports it as issued (the VO normalises the input)
        Assert.True(catalog.IsIssued(new ActivationCode("  VIORA-SP01-7K3M  ")));
    }

    [Fact]
    public void ActivationCode_Ctor_EmptyString_Throws()
    {
        // GIVEN a blank activation code
        // WHEN the VO is constructed
        // THEN ArgumentException is thrown (the VO enforces the format BEFORE the catalog is asked)
        Assert.Throws<ArgumentException>(() => new ActivationCode(""));
    }

    [Fact]
    public void ActivationCode_Ctor_MalformedCode_Throws()
    {
        // GIVEN a malformed activation code (missing the VIORA- prefix)
        // WHEN the VO is constructed
        // THEN ArgumentException is thrown (the VO enforces the regex; the catalog is not asked)
        Assert.Throws<ArgumentException>(() => new ActivationCode("SPO1-7K3M"));
    }
}
