namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The sign-in resource
 * </summary>
 * <remarks>
 *     This resource represents the data required to sign in a user.
 * </remarks>
 */
public record SignInResource(string Username, string Password);
