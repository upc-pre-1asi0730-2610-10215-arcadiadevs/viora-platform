namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;

/// <summary>
///     Strongly-typed options driving the dynamic-nutrition policy used by
///     the <c>IYieldForecastEstimator</c> (A1, this PR) and, in a future
///     delivery, the <c>IDynamicNutritionPlanGenerator</c> (PR-D2). Bound
///     from the <c>Agronomic:DynamicNutrition</c> configuration section and
///     validated at startup by
///     <see cref="DynamicNutritionPolicyOptionsValidator"/> (CC-5 fail-fast).
/// </summary>
/// <remarks>
///     <para>
///         The 8 fields mirror the OS <c>DynamicNutritionPolicy.java</c>
///         shape verbatim (one record / one config class; single source of
///         truth for the policy). Field names use C# PascalCase; the JSON
///         section uses camelCase so <c>Options pattern</c> binding still
///         matches the design's intent.
///     </para>
///     <para>
///         <strong>Why this class lives in <c>Infrastructure/Configuration</c></strong>:
///         it is the I/O-side config-binding surface, not a domain value
///         object. The future <c>DynamicNutritionPolicy</c> record (PR-D2)
///         will be the validated, immutable domain VO that gets passed to
///         the generator; this class is the JSON-shaped carrier the IOptions
///         pattern binds to.
///     </para>
/// </remarks>
public sealed class DynamicNutritionPolicyOptions
{
    /// <summary>
    ///     The configuration section path used for binding. Used by
    ///     <c>Program.cs</c> to wire <c>AddOptionsWithValidateOnStart</c>.
    /// </summary>
    public const string SectionName = "Agronomic:DynamicNutrition";

    /// <summary>
    ///     Reference temperature (Celsius) for anomaly calculation. Must be
    ///     a finite number.
    /// </summary>
    public decimal TemperatureReferenceCelsius { get; set; } = 20.0m;

    /// <summary>
    ///     NDVI value (range -1..1) below which vegetation risk is
    ///     classified as <c>High</c>. Must be strictly less than
    ///     <see cref="ModerateRiskNdviThreshold"/>.
    /// </summary>
    public decimal HighRiskNdviThreshold { get; set; } = 0.30m;

    /// <summary>
    ///     NDVI value (range -1..1) below which vegetation risk is
    ///     classified as <c>Moderate</c>. Must be strictly greater than
    ///     <see cref="HighRiskNdviThreshold"/>.
    /// </summary>
    public decimal ModerateRiskNdviThreshold { get; set; } = 0.50m;

    /// <summary>
    ///     Number of days the application window is open when the climate
    ///     risk level is <c>High</c>. Must be at least 1.
    /// </summary>
    public int HighRiskWindowDays { get; set; } = 14;

    /// <summary>
    ///     Number of days the application window is open when the climate
    ///     risk level is <c>Extreme</c>. Must be at least 1.
    /// </summary>
    public int ExtremeRiskWindowDays { get; set; } = 21;

    /// <summary>
    ///     Foliar support dosage (litres per hectare). Must be positive.
    /// </summary>
    public decimal FoliarSupportDosageLitersPerHectare { get; set; } = 2.5m;

    /// <summary>
    ///     Potassium-calcium dosage (kilograms per hectare). Must be
    ///     positive.
    /// </summary>
    public decimal PotassiumCalciumDosageKilogramsPerHectare { get; set; } = 3.0m;

    /// <summary>
    ///     Biostimulant dosage (litres per hectare). Must be positive.
    /// </summary>
    public decimal BiostimulantDosageLitersPerHectare { get; set; } = 1.2m;

    /// <summary>
    ///     Chill-deficit trigger ratio consumed by
    ///     <c>IChillDeficitEvaluator</c> (A2 part 1, PR-D1). A chill
    ///     deficit is reported when
    ///     <c>accumulatedChillPortions &lt; chillRequirement.Portions × ChillDeficitRatio</c>.
    ///     Must be inside the closed interval [0, 1]. Default 0.7
    ///     (the OS default; ~70 % of the requirement is enough to
    ///     consider the requirement met).
    /// </summary>
    public decimal ChillDeficitRatio { get; set; } = 0.7m;
}
