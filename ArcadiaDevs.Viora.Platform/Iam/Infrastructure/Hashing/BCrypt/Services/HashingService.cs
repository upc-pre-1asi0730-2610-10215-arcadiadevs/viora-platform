using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using BCryptNet = BCrypt.Net.BCrypt;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;

/**
 * <summary>
 *     This class is responsible for hashing and validating passwords.
 * </summary>
 */
public class HashingService : IHashingService
{
    /**
     * <summary>
     *     This method hashes a password with a work factor of 11.
     * </summary>
     * <param name="password">The password to hash.</param>
     * <returns>The hashed password.</returns>
     */
    public string HashPassword(string password)
    {
        return BCryptNet.HashPassword(password, 11);
    }

    /**
     * <summary>
     *     This method validates a password against a hash.
     * </summary>
     * <param name="password">The password to validate.</param>
     * <param name="passwordHash">The hash to validate against.</param>
     * <returns>True if the password is valid, false otherwise.</returns>
     */
    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCryptNet.Verify(password, passwordHash);
    }
}
