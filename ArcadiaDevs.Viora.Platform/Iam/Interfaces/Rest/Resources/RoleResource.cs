namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The role resource
 * </summary>
 * <remarks>
 *     This resource represents a role returned by the API.
 * </remarks>
 */
public record RoleResource(int Id, string Name, string? Description);
