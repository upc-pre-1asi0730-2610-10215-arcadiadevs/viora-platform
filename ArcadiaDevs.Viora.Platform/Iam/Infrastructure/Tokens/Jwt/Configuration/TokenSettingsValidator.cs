using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

/// <summary>
///     Validates <see cref="TokenSettings"/> at startup via IValidateOptions.
///     Rejects placeholder secrets, short secrets (&lt; 32 bytes), and missing secrets.
///     Fails fast in all environments (CC-5).
/// </summary>
public class TokenSettingsValidator : IValidateOptions<TokenSettings>
{
    private const string PlaceholderValue = "DEV-ONLY-PLEASE-CHANGE-ME";
    private const int MinimumSecretLength = 32;

    public ValidateOptionsResult Validate(string? name, TokenSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            return ValidateOptionsResult.Fail(
                "Jwt:Secret is required. Set it via the Jwt__Secret environment variable or user secrets.");
        }

        if (options.Secret == PlaceholderValue)
        {
            return ValidateOptionsResult.Fail(
                "Jwt:Secret is set to the placeholder value. Replace it with a real secret (minimum 32 bytes).");
        }

        if (options.Secret.Length < MinimumSecretLength)
        {
            return ValidateOptionsResult.Fail(
                $"Jwt:Secret must be at least {MinimumSecretLength} characters long. Current length: {options.Secret.Length}.");
        }

        return ValidateOptionsResult.Success;
    }
}
