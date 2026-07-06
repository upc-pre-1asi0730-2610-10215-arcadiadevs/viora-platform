using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.ExternalServices.Configuration;

/// <summary>
///     Validates <see cref="MercadoPagoOptions" /> at startup via
///     <see cref="IValidateOptions{TOptions}" />.
/// </summary>
/// <remarks>
///     Deliberate divergence from the <c>AgroMonitoringWeatherOptionsValidator</c>
///     precedent this composes from (design's PaymentGateway Port Design
///     section): that validator has NO off-switch — it fails startup
///     unconditionally if its API key is missing (AGRO-003, fail-fast
///     always). Billing genuinely needs an off switch (REQ-GATE-2 — the app
///     MUST build/run with zero real credentials), so this validator only
///     requires <see cref="MercadoPagoOptions.AccessToken" /> to be
///     non-blank when <see cref="MercadoPagoOptions.Enabled" /> is
///     <c>true</c>. <c>Enabled == false</c> always validates successfully,
///     regardless of <c>AccessToken</c>'s value.
/// </remarks>
public class MercadoPagoOptionsValidator : IValidateOptions<MercadoPagoOptions>
{
    /// <summary>
    ///     Validates the supplied options.
    /// </summary>
    /// <param name="name">The named options instance name (unused).</param>
    /// <param name="options">The options to validate.</param>
    /// <returns>
    ///     <see cref="ValidateOptionsResult.Success" /> when disabled, or
    ///     when enabled with a non-blank <c>AccessToken</c>;
    ///     <see cref="ValidateOptionsResult.Fail" /> otherwise.
    /// </returns>
    public ValidateOptionsResult Validate(string? name, MercadoPagoOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail(
                $"{MercadoPagoOptions.SectionName} is required. " +
                "Bind the section or leave Enabled=false to run without MercadoPago.");
        }

        if (!options.Enabled)
        {
            // Off-switch: unconfigured-but-valid startup state (REQ-GATE-2).
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return ValidateOptionsResult.Fail(
                $"{MercadoPagoOptions.SectionName}:AccessToken is required when " +
                $"{MercadoPagoOptions.SectionName}:Enabled is true. " +
                "Set the AccessToken via appsettings.json, environment variable " +
                "(Billing__PaymentGateway__MercadoPago__AccessToken), or user secrets.");
        }

        return ValidateOptionsResult.Success;
    }
}
