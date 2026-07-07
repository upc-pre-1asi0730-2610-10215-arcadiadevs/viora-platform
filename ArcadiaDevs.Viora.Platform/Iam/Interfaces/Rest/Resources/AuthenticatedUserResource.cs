namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The authenticated user resource
 * </summary>
 * <remarks>
 *     This resource represents an authenticated user with a JWT token.
 * </remarks>
 */
public record AuthenticatedUserResource(int Id, string Username, string Token, string Role);
