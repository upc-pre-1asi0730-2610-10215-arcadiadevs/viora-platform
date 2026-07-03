namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The sign-up resource
 * </summary>
 * <remarks>
 *     This resource represents the data required to sign up a new user.
 *     Role is optional and defaults to "Grower" when omitted or blank,
 *     mirroring OS's SignUpResource.role + Role.getDefaultRole() contract.
 * </remarks>
 */
public record SignUpResource(string Username, string Password, string Email, string FullName, string? Role = null);
