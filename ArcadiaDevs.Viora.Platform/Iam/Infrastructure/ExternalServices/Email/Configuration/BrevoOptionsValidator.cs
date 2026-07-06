using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.ExternalServices.Email.Configuration;

/// <summary>
///     Validates <see cref="BrevoOptions" /> at startup via
///     <see cref="IValidateOptions{TOptions}" />. Identical shape to
///     <c>MercadoPagoOptionsValidator</c> — only requires
///     <see cref="BrevoOptions.ApiKey" /> to be non-blank when
///     <see cref="BrevoOptions.Enabled" /> is <c>true</c>.
///     <c>Enabled == false</c> always validates successfully.
/// </summary>
public class BrevoOptionsValidator : IValidateOptions<BrevoOptions>
{
    /// <summary>
    ///     Validates the supplied options.
    /// </summary>
    public ValidateOptionsResult Validate(string? name, BrevoOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail(
                $"{BrevoOptions.SectionName} is required. " +
                "Bind the section or leave Enabled=false to run without Brevo.");
        }

        if (!options.Enabled)
        {
            // Off-switch: unconfigured-but-valid startup state (REQ-EMAIL-2).
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail(
                $"{BrevoOptions.SectionName}:ApiKey is required when " +
                $"{BrevoOptions.SectionName}:Enabled is true. " +
                "Set the ApiKey via appsettings.json, environment variable " +
                "(Iam__Email__Brevo__ApiKey), or user secrets.");
        }

        return ValidateOptionsResult.Success;
    }
}
