namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model;

/**
 * <summary>
 *     Represents an authenticated user with a token.
 * </summary>
 * <remarks>
 *     This record is used to return the result of a successful sign-in.
 * </remarks>
 */
public record AuthenticatedUser(int Id, string Username, string Token);
