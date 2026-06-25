using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;

/**
 * <summary>
 *     Result of JWT token validation, distinguishing between valid, expired, and invalid tokens.
 * </summary>
 */
public record JwtValidationResult(bool IsValid, int? UserId, string? FailureCode)
{
    public static JwtValidationResult Success(int userId) => new(true, userId, null);
    public static JwtValidationResult Failure(string code) => new(false, null, code);
    public static JwtValidationResult Expired => new(false, null, "Iam.TokenExpired");
    public static JwtValidationResult Invalid => new(false, null, "Iam.TokenInvalid");
}

/**
 * <summary>
 *     The token service interface
 * </summary>
 * <remarks>
 *     This interface is used to generate and validate JWT tokens
 * </remarks>
 */
public interface ITokenService
{
    /**
     * <summary>
     *     Generate a JWT token
     * </summary>
     * <param name="user">The user to generate the token for</param>
     * <returns>The generated token</returns>
     */
    string GenerateToken(User user);

    /**
     * <summary>
     *     Validate a JWT token
     * </summary>
     * <param name="token">The token to validate</param>
     * <returns>A JwtValidationResult indicating success, expiry, or other failure</returns>
     */
    Task<JwtValidationResult> ValidateToken(string token);
}
