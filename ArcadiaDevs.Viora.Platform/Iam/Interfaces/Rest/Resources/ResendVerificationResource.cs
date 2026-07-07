namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The resend-verification resource
 * </summary>
 * <remarks>
 *     The login identifier for this endpoint is <c>Username</c>, not
 *     <c>email</c>.
 * </remarks>
 */
public record ResendVerificationResource(string Username);
