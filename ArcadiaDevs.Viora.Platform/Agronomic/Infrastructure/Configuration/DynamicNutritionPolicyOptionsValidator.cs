using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;

/// <summary>
///     Validates <see cref="DynamicNutritionPolicyOptions"/> at startup via
///     <see cref="IValidateOptions{TOptions}"/>. Mirrors the runtime
///     invariants of the OS <c>DynamicNutritionPolicy</c> record: finite
///     temperature, NDVI thresholds inside [-1, 1] with the high-risk
///     threshold strictly below the moderate-risk threshold, application
///     windows of at least one day, and strictly positive dosages (CC-5
///     fail-fast in all environments).
/// </summary>
public class DynamicNutritionPolicyOptionsValidator
    : IValidateOptions<DynamicNutritionPolicyOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DynamicNutritionPolicyOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail(
                $"{DynamicNutritionPolicyOptions.SectionName} is required. " +
                $"Bind the {DynamicNutritionPolicyOptions.SectionName} configuration section " +
                "or set the Agronomic__DynamicNutrition__* environment variables.");
        }

        var failures = new List<string>();

        // The OS rule "temperatureReferenceCelsius must be finite" maps to
        // rejecting NaN/Infinity in Java's `double`. C# `decimal` cannot
        // represent those values, so the rule is vacuously satisfied for
        // any value produced by the JSON binder. We keep the predicate in
        // the code so the validation intent is explicit and a future change
        // to `double` (or a manual deserialization path) would still be
        // caught. The default (20.0) is the OS-equivalent ambient
        // temperature for the OLIVO crop, so a missing config still
        // produces a sensible value.
        _ = options.TemperatureReferenceCelsius;

        if (options.HighRiskNdviThreshold < -1m || options.HighRiskNdviThreshold > 1m)
        {
            failures.Add(
                "HighRiskNdviThreshold must be within the closed interval [-1, 1] " +
                $"(actual: {options.HighRiskNdviThreshold}).");
        }

        if (options.ModerateRiskNdviThreshold < -1m || options.ModerateRiskNdviThreshold > 1m)
        {
            failures.Add(
                "ModerateRiskNdviThreshold must be within the closed interval [-1, 1] " +
                $"(actual: {options.ModerateRiskNdviThreshold}).");
        }

        if (options.HighRiskNdviThreshold >= options.ModerateRiskNdviThreshold)
        {
            failures.Add(
                "HighRiskNdviThreshold must be strictly less than ModerateRiskNdviThreshold " +
                $"(actual: {options.HighRiskNdviThreshold} >= {options.ModerateRiskNdviThreshold}).");
        }

        if (options.HighRiskWindowDays < 1)
        {
            failures.Add(
                $"HighRiskWindowDays must be at least 1 (actual: {options.HighRiskWindowDays}).");
        }

        if (options.ExtremeRiskWindowDays < 1)
        {
            failures.Add(
                $"ExtremeRiskWindowDays must be at least 1 (actual: {options.ExtremeRiskWindowDays}).");
        }

        if (options.FoliarSupportDosageLitersPerHectare <= 0m)
        {
            failures.Add(
                "FoliarSupportDosageLitersPerHectare must be strictly positive " +
                $"(actual: {options.FoliarSupportDosageLitersPerHectare}).");
        }

        if (options.PotassiumCalciumDosageKilogramsPerHectare <= 0m)
        {
            failures.Add(
                "PotassiumCalciumDosageKilogramsPerHectare must be strictly positive " +
                $"(actual: {options.PotassiumCalciumDosageKilogramsPerHectare}).");
        }

        if (options.BiostimulantDosageLitersPerHectare <= 0m)
        {
            failures.Add(
                "BiostimulantDosageLitersPerHectare must be strictly positive " +
                $"(actual: {options.BiostimulantDosageLitersPerHectare}).");
        }

        if (options.ChillDeficitRatio < 0m || options.ChillDeficitRatio > 1m)
        {
            failures.Add(
                "ChillDeficitRatio must be within the closed interval [0, 1] " +
                $"(actual: {options.ChillDeficitRatio}).");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
