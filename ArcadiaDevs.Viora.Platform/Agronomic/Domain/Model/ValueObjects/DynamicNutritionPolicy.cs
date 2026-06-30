namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Validated, immutable dynamic-nutrition policy value object. Mirrors
///     the OS <c>DynamicNutritionPolicy.java</c> shape (one record / one
///     config class; single source of truth for the policy) and adds the
///     9th field <see cref="ChillDeficitRatio"/> consumed by
///     <c>IChillDeficitEvaluator</c> in this PR (A2 part 1).
///     <para>
///         The I/O-side JSON shape is <c>DynamicNutritionPolicyOptions</c>
///         (introduced in PR-C, <c>Infrastructure/Configuration</c>); the
///         generator in PR-D2 will convert the options into this VO at the
///         boundary so the math runs against a validated, immutable value
///         type. The 3 evaluators introduced in this PR read the
///         <c>IOptions</c>-bound options class directly because they only
///         need one or two fields; the VO is the canonical shape for the
///         generator contract.
///     </para>
///     <para>
///         All 8 fields from the design (§5.2.1) plus
///         <see cref="ChillDeficitRatio"/> are validated in the primary
///         constructor (finite temperature; NDVI thresholds inside
///         [-1, 1] with <c>HighRiskNdviThreshold &lt; ModerateRiskNdviThreshold</c>;
///         window days at least 1; all dosages strictly positive; ratio in
///         [0, 1]). The validation intent mirrors the OS record so a
///         future refactor that converts from <c>DynamicNutritionPolicyOptions</c>
///         cannot produce an invalid policy.
///     </para>
/// </summary>
public sealed record DynamicNutritionPolicy
{
    /// <summary>
    ///     Reference temperature (Celsius) for anomaly calculation. Must be
    ///     a finite number.
    /// </summary>
    public decimal TemperatureReferenceCelsius { get; }

    /// <summary>
    ///     NDVI value (range -1..1) below which vegetation risk is
    ///     classified as <c>High</c>. Must be strictly less than
    ///     <see cref="ModerateRiskNdviThreshold"/>.
    /// </summary>
    public decimal HighRiskNdviThreshold { get; }

    /// <summary>
    ///     NDVI value (range -1..1) below which vegetation risk is
    ///     classified as <c>Moderate</c>. Must be strictly greater than
    ///     <see cref="HighRiskNdviThreshold"/>.
    /// </summary>
    public decimal ModerateRiskNdviThreshold { get; }

    /// <summary>
    ///     Number of days the application window is open when the climate
    ///     risk level is <c>High</c>. Must be at least 1.
    /// </summary>
    public int HighRiskWindowDays { get; }

    /// <summary>
    ///     Number of days the application window is open when the climate
    ///     risk level is <c>Extreme</c>. Must be at least 1.
    /// </summary>
    public int ExtremeRiskWindowDays { get; }

    /// <summary>
    ///     Foliar support dosage (litres per hectare). Must be positive.
    /// </summary>
    public decimal FoliarSupportDosageLitersPerHectare { get; }

    /// <summary>
    ///     Potassium-calcium dosage (kilograms per hectare). Must be
    ///     positive.
    /// </summary>
    public decimal PotassiumCalciumDosageKilogramsPerHectare { get; }

    /// <summary>
    ///     Biostimulant dosage (litres per hectare). Must be positive.
    /// </summary>
    public decimal BiostimulantDosageLitersPerHectare { get; }

    /// <summary>
    ///     Chill-deficit trigger ratio. A chill deficit is reported when
    ///     <c>accumulatedChillPortions &lt; chillRequirement.Portions × ChillDeficitRatio</c>
    ///     (see <c>IChillDeficitEvaluator.HasDeficit</c>). Defaults to
    ///     <c>0.7</c> (i.e. 70 % of the requirement); must be inside the
    ///     closed interval [0, 1].
    /// </summary>
    public decimal ChillDeficitRatio { get; }

    /// <summary>
    ///     Builds a validated <see cref="DynamicNutritionPolicy"/> value
    ///     object.
    /// </summary>
    /// <param name="temperatureReferenceCelsius">Reference temperature in Celsius; must be finite.</param>
    /// <param name="highRiskNdviThreshold">NDVI threshold for high risk; must be inside [-1, 1] and strictly less than <paramref name="moderateRiskNdviThreshold"/>.</param>
    /// <param name="moderateRiskNdviThreshold">NDVI threshold for moderate risk; must be inside [-1, 1] and strictly greater than <paramref name="highRiskNdviThreshold"/>.</param>
    /// <param name="highRiskWindowDays">Application window (days) for high climate risk; must be at least 1.</param>
    /// <param name="extremeRiskWindowDays">Application window (days) for extreme climate risk; must be at least 1.</param>
    /// <param name="foliarSupportDosageLitersPerHectare">Foliar support dosage; must be positive.</param>
    /// <param name="potassiumCalciumDosageKilogramsPerHectare">Potassium-calcium dosage; must be positive.</param>
    /// <param name="biostimulantDosageLitersPerHectare">Biostimulant dosage; must be positive.</param>
    /// <param name="chillDeficitRatio">Chill-deficit trigger ratio; must be inside [0, 1].</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when any of the numeric constraints are violated. C#
    ///     <c>decimal</c> cannot represent <c>NaN</c> or <c>Infinity</c>, so
    ///     the "finite temperature" rule is vacuously satisfied for any
    ///     value produced by the JSON binder.
    /// </exception>
    public DynamicNutritionPolicy(
        decimal temperatureReferenceCelsius,
        decimal highRiskNdviThreshold,
        decimal moderateRiskNdviThreshold,
        int highRiskWindowDays,
        int extremeRiskWindowDays,
        decimal foliarSupportDosageLitersPerHectare,
        decimal potassiumCalciumDosageKilogramsPerHectare,
        decimal biostimulantDosageLitersPerHectare,
        decimal chillDeficitRatio)
    {
        if (highRiskNdviThreshold < -1m || highRiskNdviThreshold > 1m)
        {
            throw new ArgumentException(
                "HighRiskNdviThreshold must be within the closed interval [-1, 1] " +
                $"(actual: {highRiskNdviThreshold}).",
                nameof(highRiskNdviThreshold));
        }

        if (moderateRiskNdviThreshold < -1m || moderateRiskNdviThreshold > 1m)
        {
            throw new ArgumentException(
                "ModerateRiskNdviThreshold must be within the closed interval [-1, 1] " +
                $"(actual: {moderateRiskNdviThreshold}).",
                nameof(moderateRiskNdviThreshold));
        }

        if (highRiskNdviThreshold >= moderateRiskNdviThreshold)
        {
            throw new ArgumentException(
                "HighRiskNdviThreshold must be strictly less than ModerateRiskNdviThreshold " +
                $"(actual: {highRiskNdviThreshold} >= {moderateRiskNdviThreshold}).");
        }

        if (highRiskWindowDays < 1)
        {
            throw new ArgumentException(
                $"HighRiskWindowDays must be at least 1 (actual: {highRiskWindowDays}).",
                nameof(highRiskWindowDays));
        }

        if (extremeRiskWindowDays < 1)
        {
            throw new ArgumentException(
                $"ExtremeRiskWindowDays must be at least 1 (actual: {extremeRiskWindowDays}).",
                nameof(extremeRiskWindowDays));
        }

        if (foliarSupportDosageLitersPerHectare <= 0m)
        {
            throw new ArgumentException(
                "FoliarSupportDosageLitersPerHectare must be strictly positive " +
                $"(actual: {foliarSupportDosageLitersPerHectare}).",
                nameof(foliarSupportDosageLitersPerHectare));
        }

        if (potassiumCalciumDosageKilogramsPerHectare <= 0m)
        {
            throw new ArgumentException(
                "PotassiumCalciumDosageKilogramsPerHectare must be strictly positive " +
                $"(actual: {potassiumCalciumDosageKilogramsPerHectare}).",
                nameof(potassiumCalciumDosageKilogramsPerHectare));
        }

        if (biostimulantDosageLitersPerHectare <= 0m)
        {
            throw new ArgumentException(
                "BiostimulantDosageLitersPerHectare must be strictly positive " +
                $"(actual: {biostimulantDosageLitersPerHectare}).",
                nameof(biostimulantDosageLitersPerHectare));
        }

        if (chillDeficitRatio < 0m || chillDeficitRatio > 1m)
        {
            throw new ArgumentException(
                "ChillDeficitRatio must be within the closed interval [0, 1] " +
                $"(actual: {chillDeficitRatio}).",
                nameof(chillDeficitRatio));
        }

        TemperatureReferenceCelsius = temperatureReferenceCelsius;
        HighRiskNdviThreshold = highRiskNdviThreshold;
        ModerateRiskNdviThreshold = moderateRiskNdviThreshold;
        HighRiskWindowDays = highRiskWindowDays;
        ExtremeRiskWindowDays = extremeRiskWindowDays;
        FoliarSupportDosageLitersPerHectare = foliarSupportDosageLitersPerHectare;
        PotassiumCalciumDosageKilogramsPerHectare = potassiumCalciumDosageKilogramsPerHectare;
        BiostimulantDosageLitersPerHectare = biostimulantDosageLitersPerHectare;
        ChillDeficitRatio = chillDeficitRatio;
    }
}
