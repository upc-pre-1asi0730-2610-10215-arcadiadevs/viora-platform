using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="AgronomicRiskTranslator"/>
///     (A2 part 2 / PR-D2). The translator is a pure mapping from the
///     per-risk boolean signals (chillDeficit, lowNdvi, hydricStress) and
///     the snapshot's <see cref="ClimateRiskLevel"/> to the
///     <see cref="EThreatType"/> set consumed by
///     <see cref="DynamicNutritionPlanGenerator"/>. The mapping is
///     deterministic: <c>Critical</c> produces both
///     <see cref="EThreatType.ClimateHigh"/> and
///     <see cref="EThreatType.ClimateExtreme"/> (the OS pattern:
///     "Critical includes both High and Extreme"); <c>High</c> produces
///     only ClimateHigh; <c>Medium</c> / <c>Low</c> produce no climate
///     risk on their own (the per-risk evaluators carry the load).
/// </summary>
public class AgronomicRiskTranslatorTests
{
    private static AgronomicRiskTranslator BuildTranslator() => new();

    [Fact]
    public void Translate_CriticalClimate_EmitsBothHighAndExtreme()
    {
        // GIVEN a Critical climate level (the OS pattern: Critical includes both High and Extreme)
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Critical, chillDeficit: false, lowNdvi: false, hydricStress: false);

        // THEN both ClimateHigh and ClimateExtreme are in the set
        Assert.Contains(EThreatType.ClimateHigh, risks);
        Assert.Contains(EThreatType.ClimateExtreme, risks);
        Assert.Equal(2, risks.Count);
    }

    [Fact]
    public void Translate_HighClimate_EmitsHighOnly()
    {
        // GIVEN a High climate level
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.High, false, false, false);

        // THEN only ClimateHigh is in the set (not ClimateExtreme)
        Assert.Contains(EThreatType.ClimateHigh, risks);
        Assert.DoesNotContain(EThreatType.ClimateExtreme, risks);
        Assert.Single(risks);
    }

    [Fact]
    public void Translate_MediumClimate_EmitsNoClimateRisk()
    {
        // GIVEN a Medium climate level (no climate risk; the per-risk evaluators carry the load)
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Medium, false, false, false);

        // THEN no climate risk is in the set
        Assert.DoesNotContain(EThreatType.ClimateHigh, risks);
        Assert.DoesNotContain(EThreatType.ClimateExtreme, risks);
        Assert.Empty(risks);
    }

    [Fact]
    public void Translate_LowClimate_EmitsNoClimateRisk()
    {
        // GIVEN a Low climate level
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Low, false, false, false);

        // THEN no climate risk is in the set
        Assert.DoesNotContain(EThreatType.ClimateHigh, risks);
        Assert.DoesNotContain(EThreatType.ClimateExtreme, risks);
        Assert.Empty(risks);
    }

    [Fact]
    public void Translate_ChillDeficitTrue_EmitsChillDeficit()
    {
        // GIVEN a true chillDeficit boolean (and nothing else)
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Low, chillDeficit: true, lowNdvi: false, hydricStress: false);

        // THEN ChillDeficit is in the set
        Assert.Contains(EThreatType.ChillDeficit, risks);
        Assert.Single(risks);
    }

    [Fact]
    public void Translate_ChillDeficitFalse_OmitsChillDeficit()
    {
        // GIVEN a false chillDeficit boolean
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Low, chillDeficit: false, lowNdvi: false, hydricStress: false);

        // THEN ChillDeficit is NOT in the set
        Assert.DoesNotContain(EThreatType.ChillDeficit, risks);
    }

    [Fact]
    public void Translate_LowNdviTrue_EmitsLowNdvi()
    {
        // GIVEN a true lowNdvi boolean
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Low, chillDeficit: false, lowNdvi: true, hydricStress: false);

        // THEN LowNdvi is in the set
        Assert.Contains(EThreatType.LowNdvi, risks);
        Assert.Single(risks);
    }

    [Fact]
    public void Translate_HydricStressTrue_EmitsHydricStress()
    {
        // GIVEN a true hydricStress boolean
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Low, chillDeficit: false, lowNdvi: false, hydricStress: true);

        // THEN HydricStress is in the set
        Assert.Contains(EThreatType.HydricStress, risks);
        Assert.Single(risks);
    }

    [Fact]
    public void Translate_AllSignalsOn_EmitsFiveRisks()
    {
        // GIVEN a Critical climate + all 3 per-risk booleans true
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Critical, chillDeficit: true, lowNdvi: true, hydricStress: true);

        // THEN the set contains all 5 risk codes
        Assert.Contains(EThreatType.ClimateHigh, risks);
        Assert.Contains(EThreatType.ClimateExtreme, risks);
        Assert.Contains(EThreatType.ChillDeficit, risks);
        Assert.Contains(EThreatType.LowNdvi, risks);
        Assert.Contains(EThreatType.HydricStress, risks);
        Assert.Equal(5, risks.Count);
    }

    [Fact]
    public void Translate_AllSignalsOff_EmitsEmpty()
    {
        // GIVEN no signals
        var translator = BuildTranslator();

        // WHEN the translator runs
        var risks = translator.Translate(ClimateRiskLevel.Low, chillDeficit: false, lowNdvi: false, hydricStress: false);

        // THEN the set is empty (the generator will throw CC-7 on empty)
        Assert.Empty(risks);
    }
}
