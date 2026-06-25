namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The sign-up resource
 * </summary>
 * <remarks>
 *     This resource represents the data required to sign up a new user.
 *     S1: no roles — role assignment is a separate S2 operation.
 * </remarks>
 */
public record SignUpResource(string Username, string Password);
