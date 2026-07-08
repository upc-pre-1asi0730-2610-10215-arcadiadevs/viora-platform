namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The update-user resource
 * </summary>
 * <remarks>
 *     Danger-zone semantics (REQ-DEACT-2) — only <c>{ "active": false }</c> is
 *     accepted. Supplying <c>active: true</c> or omitting <c>active</c> is
 *     rejected: this endpoint has no reactivation path.
 * </remarks>
 */
public record UpdateUserResource(bool? Active);
