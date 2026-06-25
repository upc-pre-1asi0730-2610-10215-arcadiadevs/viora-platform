namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The user resource
 * </summary>
 * <remarks>
 *     This resource represents a user returned by the API.
 *     S1: no Roles field — roles are assigned in S2 via a separate endpoint.
 * </remarks>
 */
public record UserResource(int Id, string Username);
