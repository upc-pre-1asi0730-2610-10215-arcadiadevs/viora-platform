using System.Security.Claims;
using System.Text;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
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
public class TokenService(IOptions<TokenSettings> tokenSettings) : ITokenService
{
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
            Expires = DateTime.UtcNow.AddDays(7),
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
            ClockSkew = TimeSpan.Zero
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
}
