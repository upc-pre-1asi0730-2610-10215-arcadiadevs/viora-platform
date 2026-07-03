using System.Security.Claims;
using System.Text;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Services;

/**
 * <summary>
 *     The token service
 * </summary>
 * <remarks>
 *     This class is used to generate and validate tokens
 * </remarks>
 */
public class TokenService(IOptions<TokenSettings> tokenSettings, IClock clock) : ITokenService
{
    /// <summary>
    ///     Backward-compatible constructor that defaults to <see cref="SystemClock"/>.
    /// </summary>
    public TokenService(IOptions<TokenSettings> tokenSettings)
        : this(tokenSettings, new Shared.Infrastructure.SystemClock())
    {
    }

    private readonly TokenSettings _tokenSettings = tokenSettings.Value;

    /**
     * <summary>
     *     Generate token
     * </summary>
     * <param name="user">The user for token generation</param>
     * <returns>The generated Token</returns>
     */
    public string GenerateToken(User user)
    {
        // Secret length / placeholder / empty checks are enforced at startup by
        // TokenSettingsValidator (SHARED-003 — fail-fast in all environments
        // via IValidateOptions<TokenSettings>). No need to re-check here; the
        // host cannot reach this point with an invalid secret.
        var secret = _tokenSettings.Secret;
        var key = Encoding.ASCII.GetBytes(secret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Sid, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
        };

        // Add one role claim per assigned role
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = clock.UtcNow.AddDays(7),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JsonWebTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return token;
    }

    /**
     * <summary>
     *     Validate token
     * </summary>
     * <param name="token">The token to validate</param>
     * <returns>A JwtValidationResult indicating success, expiry, or other failure</returns>
     */
    public async Task<JwtValidationResult> ValidateToken(string token)
    {
        // If token is null or empty
        if (string.IsNullOrEmpty(token))
            return JwtValidationResult.Invalid;

        // Otherwise, perform validation
        var tokenHandler = new JsonWebTokenHandler();
        var key = Encoding.ASCII.GetBytes(_tokenSettings.Secret);
        var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            // Expiration without delay
            ClockSkew = TimeSpan.Zero,
            // Microsoft.IdentityModel.Tokens 8.18.0 has no TokenValidationParameters.TimeProvider
            // property, so the injected IClock cannot be wired through the built-in expiry check.
            // LifetimeValidator is the only extension point that lets us substitute clock.UtcNow
            // for DateTime.UtcNow, which is required so tests can simulate token expiry
            // deterministically instead of depending on real wall-clock time.
            LifetimeValidator = ValidateLifetimeUsingClock
        });

        // Note: JsonWebTokenHandler.ValidateTokenAsync does NOT throw on validation
        // failure — it returns a result with IsValid=false and Exception populated.
        // We must inspect the result instead of relying on catch blocks.
        if (tokenValidationResult.IsValid)
        {
            var jwtToken = (JsonWebToken)tokenValidationResult.SecurityToken;
            var userId = int.Parse(jwtToken.Claims.First(claim => claim.Type == ClaimTypes.Sid).Value);
            return JwtValidationResult.Success(userId);
        }
        if (tokenValidationResult.Exception is SecurityTokenExpiredException)
        {
            return JwtValidationResult.Expired;
        }
        return JwtValidationResult.Invalid;
    }

    /**
     * <summary>
     *     Lifetime validator wired into <see cref="ValidateToken" /> so expiry is checked
     *     against the injected <see cref="IClock" /> instead of the real wall clock.
     * </summary>
     * <remarks>
     *     Mirrors the behavior of the built-in <c>Validators.ValidateLifetime</c> implementation,
     *     but substitutes <c>clock.UtcNow</c> for <c>DateTime.UtcNow</c>. Note that when a custom
     *     <see cref="TokenValidationParameters.LifetimeValidator" /> is supplied, the framework
     *     only throws a generic <see cref="SecurityTokenInvalidLifetimeException" /> if the
     *     delegate returns <see langword="false" /> — it does NOT surface
     *     <see cref="SecurityTokenExpiredException" /> automatically. Since the calling code in
     *     <see cref="ValidateToken" /> specifically checks for <see cref="SecurityTokenExpiredException" />
     *     to distinguish "expired" from "otherwise invalid", this method throws that exception type
     *     directly instead of returning <see langword="false" />.
     * </remarks>
     */
    private bool ValidateLifetimeUsingClock(
        DateTime? notBefore,
        DateTime? expires,
        SecurityToken securityToken,
        TokenValidationParameters validationParameters)
    {
        var now = clock.UtcNow;

        if (notBefore.HasValue && expires.HasValue && notBefore.Value > expires.Value)
        {
            throw new SecurityTokenInvalidLifetimeException(
                "Invalid lifetime: notBefore is after expires.")
            {
                NotBefore = notBefore,
                Expires = expires
            };
        }

        if (notBefore.HasValue && notBefore.Value > now.Add(validationParameters.ClockSkew))
        {
            throw new SecurityTokenNotYetValidException("Token is not yet valid.")
            {
                NotBefore = notBefore.Value
            };
        }

        if (expires.HasValue && expires.Value < now.Add(-validationParameters.ClockSkew))
        {
            throw new SecurityTokenExpiredException("Token has expired.")
            {
                Expires = expires.Value
            };
        }

        return true;
    }
}
