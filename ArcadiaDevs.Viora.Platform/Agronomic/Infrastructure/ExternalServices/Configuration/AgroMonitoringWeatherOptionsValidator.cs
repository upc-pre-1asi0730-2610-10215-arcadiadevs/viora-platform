using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices.Configuration;

/// <summary>
///     Validates <see cref="AgroMonitoringWeatherOptions"/> at startup via
///     <see cref="IValidateOptions{TOptions}"/>. Rejects a missing, empty, or
///     whitespace-only API key (CC-5 fail-fast in all environments).
/// </summary>
/// <remarks>
///     AGRO-003: AgroMonitoring is the sole weather provider in v1. There is
///     no fabricated-data fallback. A missing API key MUST cause startup to
///     fail so the production path never silently returns hard-coded
///     constants from the previous <c>WeatherDataServiceAdapter</c>.
/// </remarks>
public class AgroMonitoringWeatherOptionsValidator : IValidateOptions<AgroMonitoringWeatherOptions>
{
    /// <summary>
    ///     Validates the supplied options.
    /// </summary>
    /// <param name="name">The named options instance name (unused).</param>
    /// <param name="options">The options to validate.</param>
    /// <returns>
    ///     <see cref="ValidateOptionsResult.Success"/> when the API key is
    ///     present and non-empty; <see cref="ValidateOptionsResult.Fail"/>
    ///     with a clear, actionable error message otherwise.
    /// </returns>
    public ValidateOptionsResult Validate(string? name, AgroMonitoringWeatherOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail(
                "Agronomic:Weather:AgroMonitoring:ApiKey is required. " +
                "Bind the Agronomic:Weather:AgroMonitoring configuration section " +
                "or set the Agronomic__Weather__AgroMonitoring__ApiKey environment variable.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                "Agronomic:Weather:AgroMonitoring:ApiKey is missing or empty. " +
                "AgroMonitoring is the sole weather provider in v1; " +
                "set the ApiKey via appsettings.json, environment variable " +
                "(Agronomic__Weather__AgroMonitoring__ApiKey), or user secrets.");
        }

        return ValidateOptionsResult.Success;
    }
}
