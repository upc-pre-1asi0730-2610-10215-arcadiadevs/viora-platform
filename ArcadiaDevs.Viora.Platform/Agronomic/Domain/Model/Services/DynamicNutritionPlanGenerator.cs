using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Exceptions;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Pure-function implementation of
///     <see cref="IDynamicNutritionPlanGenerator"/>. Port of the OS
///     <c>DynamicNutritionPlanGenerator.java</c> (A2 part 2 / PR-D2) with
///     the 4-risk trigger expansion per locked decision #1
///     (<c>FULL: 4 risks</c>, engram #42).
///     <para>
///         The 3 input recommendations and the window-day mapping follow
///         the OS's "foliar + K-Ca + biostimulant" triple (the WA no longer
///         emits the legacy N/P/K trio that lived in the pre-PR-D2 hard-coded
///         path). The window is <c>ExtremeRiskWindowDays</c> when
///         <see cref="EThreatType.ClimateExtreme"/> is in the risk set;
///         otherwise it is <c>HighRiskWindowDays</c> (any other risk or any
///         combination of risks that does not include ClimateExtreme).
///     </para>
///     <para>
///         The rationale summary embeds every triggering risk code (so the
///         audit trail shows what fired); the temperature anomaly is the
///         weather snapshot's current temperature minus the policy's
///         reference temperature, mirroring the OS's
///         <c>buildRationale</c> math. C# <c>decimal</c> cannot represent
///         <c>NaN</c>/<c>Infinity</c>, so the anomaly is always a finite
///         value (vacuously satisfying the OS "finite temperature" rule).
///     </para>
///     <para>
///         CC-7 contract: an empty (or null) <c>risks</c> collection throws
///         <see cref="DynamicNutritionPlanUnavailableException"/>. The
///         command service boundary catches the exception and converts it
///         to a <c>Result.Failure</c> with the
///         <c>AgronomicErrors.NoTriggeringRisk</c> error constant.
///     </para>
/// </summary>
public sealed class DynamicNutritionPlanGenerator : IDynamicNutritionPlanGenerator
{
    /// <summary>
    ///     Stable unit-of-measure labels reused for every emitted
    ///     recommendation. Kept as named constants so the values are
    ///     searchable in the unit-test goldens and the audit log.
    /// </summary>
    private const string FoliarDosageUnit = "L/ha";
    private const string PotassiumCalciumDosageUnit = "kg/ha";
    private const string BiostimulantDosageUnit = "L/ha";

    /// <summary>
    ///     Human-readable purpose strings for the 3 emitted
    ///     recommendations. Mirrors the OS's <c>buildInputRecommendations</c>
    ///     purposes; localised strings are out of scope for v1 (the
    ///     rationale + purpose are English in WA, matching the OS).
    /// </summary>
    private const string FoliarPurpose = "Improve stress response and recovery";
    private const string PotassiumCalciumPurpose = "Support flowering consistency and reduce abortion risk";
    private const string BiostimulantPurpose = "Reinforce vegetative recovery after stress periods";

    /// <summary>
    ///     Stable label for the foliar recommendation (the "value" field of
    ///     the <see cref="NutritionInputRecommendation"/> VO).
    /// </summary>
    private const string FoliarValue = "Foliar nutrition support";
    private const string PotassiumCalciumValue = "Potassium-calcium support";
    private const string BiostimulantValue = "Biostimulant support";

    /// <inheritdoc />
    public DynamicNutritionPlan GeneratePlan(
        int userId,
        int plotId,
        IReadOnlyCollection<EThreatType> risks,
        AgronomicRiskProfile profile,
        DynamicNutritionPolicy policy,
        DateTimeOffset generatedDate)
    {
        ArgumentNullException.ThrowIfNull(risks);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(policy);

        // CC-7: early throw, no silent default. An empty (or null) risk
        // collection means no triggering threat observed → no plan.
        if (risks.Count == 0)
        {
            throw new DynamicNutritionPlanUnavailableException(
                "No triggering risk was observed for the plot; a dynamic nutrition plan cannot be generated.");
        }

        var inputRecommendations = BuildInputRecommendations(policy);
        var applicationWindow = BuildApplicationWindow(risks, generatedDate, policy);
        var rationale = BuildRationale(risks, profile, policy);

        return DynamicNutritionPlan.Recommend(
            userId,
            plotId,
            inputRecommendations,
            applicationWindow,
            rationale,
            generatedDate);
    }

    /// <summary>
    ///     Builds the 3 input recommendations: foliar (Recommended),
    ///     K-Ca (Recommended), biostimulant (Optional). Dosages come
    ///     straight from the policy. The OS only added the K-Ca triple
    ///     when the climate risk was HIGH or EXTREME; in WA, per
    ///     design §5.2.2, all 3 are always emitted (the trigger is
    ///     the risk set, not the climate level alone).
    /// </summary>
    private static IReadOnlyCollection<NutritionInputRecommendation> BuildInputRecommendations(
        DynamicNutritionPolicy policy)
    {
        return new NutritionInputRecommendation[]
        {
            new(
                value: FoliarValue,
                purpose: FoliarPurpose,
                dosage: (double)policy.FoliarSupportDosageLitersPerHectare,
                dosageUnit: FoliarDosageUnit,
                status: ENutritionInputStatus.Recommended),

            new(
                value: PotassiumCalciumValue,
                purpose: PotassiumCalciumPurpose,
                dosage: (double)policy.PotassiumCalciumDosageKilogramsPerHectare,
                dosageUnit: PotassiumCalciumDosageUnit,
                status: ENutritionInputStatus.Recommended),

            new(
                value: BiostimulantValue,
                purpose: BiostimulantPurpose,
                dosage: (double)policy.BiostimulantDosageLitersPerHectare,
                dosageUnit: BiostimulantDosageUnit,
                status: ENutritionInputStatus.Optional)
        };
    }

    /// <summary>
    ///     Resolves the application window. <c>Extreme</c> risk → policy's
    ///     <c>ExtremeRiskWindowDays</c>; any other risk (or combination
    ///     thereof) → policy's <c>HighRiskWindowDays</c>. Matches the OS
    ///     semantic that the climate risk level drives the window length
    ///     when one is present, and that non-climate triggers do not
    ///     extend the window.
    /// </summary>
    private static NutritionApplicationWindow BuildApplicationWindow(
        IReadOnlyCollection<EThreatType> risks,
        DateTimeOffset generatedDate,
        DynamicNutritionPolicy policy)
    {
        var windowDays = risks.Contains(EThreatType.ClimateExtreme)
            ? policy.ExtremeRiskWindowDays
            : policy.HighRiskWindowDays;

        return new NutritionApplicationWindow(generatedDate, generatedDate.AddDays(windowDays));
    }

    /// <summary>
    ///     Composes the plan rationale. The summary embeds every
    ///     triggering <see cref="EThreatType"/> code so the audit trail
    ///     shows what fired; the temperature anomaly is the current
    ///     weather temperature minus the policy's reference. The
    ///     <see cref="ClimateRiskLevel"/> stored on the rationale is
    ///     the snapshot's level (the OS stores the snapshot's
    ///     <c>ClimateRiskLevel</c> as the <c>TriggeringRiskLevel</c>;
    ///     the WA mirrors that for parity).
    /// </summary>
    private static PlanRationale BuildRationale(
        IReadOnlyCollection<EThreatType> risks,
        AgronomicRiskProfile profile,
        DynamicNutritionPolicy policy)
    {
        var temperatureAnomaly = (double)(
            profile.WeatherSnapshot.CurrentTemperature - policy.TemperatureReferenceCelsius);

        var riskList = string.Join(", ", risks.Select(r => r.ToString()));

        var summary = string.Format(
            CultureInfo.InvariantCulture,
            "Plan generated from triggering risk(s) {0}; NDVI {1:F2}; weather observed on {2:o}; " +
                "temperature anomaly is {3:+0.0;-0.0} C against the configured agronomic reference.",
            riskList,
            profile.NdviValue.Value,
            profile.WeatherSnapshot.LastValidatedReadingAt,
            temperatureAnomaly);

        return new PlanRationale(
            summary: summary,
            triggeringRiskLevel: MapToAgronomicClimateRiskLevel(profile.WeatherSnapshot.ClimateRiskLevel),
            ndviValue: profile.NdviValue,
            temperatureAnomaly: temperatureAnomaly);
    }

    /// <summary>
    ///     Maps the snapshot's <see cref="ClimateRiskLevel"/> (Low/Medium/
    ///     High/Critical) onto the <see cref="EClimateRiskLevel"/> the
    ///     <see cref="PlanRationale"/> VO expects (Low/Moderate/High/
    ///     Extreme/Unknown). <c>Medium</c> → <c>Moderate</c>;
    ///     <c>Critical</c> → <c>Extreme</c>; the rest pass through.
    /// </summary>
    private static EClimateRiskLevel MapToAgronomicClimateRiskLevel(ClimateRiskLevel level)
    {
        return level switch
        {
            ClimateRiskLevel.Low => EClimateRiskLevel.Low,
            ClimateRiskLevel.Medium => EClimateRiskLevel.Moderate,
            ClimateRiskLevel.High => EClimateRiskLevel.High,
            ClimateRiskLevel.Critical => EClimateRiskLevel.Extreme,
            _ => EClimateRiskLevel.Unknown
        };
    }
}
