namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The resend-verification resource
 * </summary>
 * <remarks>
 *     Diverges from OS's <c>email</c> field: WA's login identifier stays
 *     <c>Username</c> (unchanged per proposal).
 * </remarks>
 */
public record ResendVerificationResource(string Username);
