namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The user-session resource
 * </summary>
 * <remarks>
 *     This resource represents a session returned by the API.
 * </remarks>
 */
public record UserSessionResource(int Id, string UserAgent, DateTime LastActiveAt, bool IsCurrent);
