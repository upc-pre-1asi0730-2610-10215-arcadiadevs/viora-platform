namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The user resource
 * </summary>
 * <remarks>
 *     This resource represents a user returned by the API.
 *     S1: no Roles field — roles are assigned in S2 via a separate endpoint.
 *     Deliberately does NOT surface Email/FullName — Profile BC remains the
 *     single source of truth for profile-facing data; Iam's copy is
 *     internal-only, for gating/notification purposes.
 * </remarks>
 */
public record UserResource(int Id, string Username, bool Active, bool Verified);
