namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     The set of risk triggers consumed by the dynamic-nutrition plan
///     generator (<c>IDynamicNutritionPlanGenerator</c>, A2 part 2 / PR-D2)
///     and the per-risk evaluators introduced in this PR (A2 part 1).
///     <para>
///         This is a BC-local enum (CC-11). It coexists with
///         <c>Surveillance.Domain.Model.ValueObjects.EThreatType</c>
///         (13 values, used as the <c>Alert.Type</c> by the Surveillance BC);
///         the C# namespaces keep them unambiguous. Use the fully-qualified
///         name at every call site that crosses BC boundaries.
///     </para>
///     <para>
///         The first two values (<see cref="ClimateHigh"/>,
///         <see cref="ClimateExtreme"/>) mirror the OS's "NDVI can only RAISE
///         risk" semantics: the climate risk level already encodes High and
///         Extreme, so the generator does not need separate enum values for
///         them — the translator
///         (<c>IAgronomicRiskTranslator</c>, PR-D2) maps
///         <c>ClimateRiskLevel == High|Extreme</c> onto this enum at the
///         boundary. The remaining three values correspond to the three
///         pure-function evaluators introduced in this PR.
///     </para>
/// </summary>
public enum EThreatType
{
    /// <summary>
    ///     Climate risk level <c>High</c> is observed (the
    ///     <c>ClimateRiskLevel</c> on the latest <c>WeatherSnapshot</c>).
    /// </summary>
    ClimateHigh = 0,

    /// <summary>
    ///     Climate risk level <c>Extreme</c> is observed.
    /// </summary>
    ClimateExtreme = 1,

    /// <summary>
    ///     Accumulated chill portions are below the
    ///     <see cref="ChillRequirement"/> threshold (the
    ///     <c>IChillDeficitEvaluator</c>).
    /// </summary>
    ChillDeficit = 2,

    /// <summary>
    ///     The latest <c>AgronomicStatistic.NdviValue</c> is below the
    ///     policy's <c>HighRiskNdviThreshold</c> (the
    ///     <c>ILowNdviEvaluator</c>).
    /// </summary>
    LowNdvi = 3,

    /// <summary>
    ///     Hot and sunny weather coincides with a low NDVI trend (the
    ///     <c>IHydricStressEvaluator</c>).
    /// </summary>
    HydricStress = 4
}
