namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The revoke-session command
 * </summary>
 * <remarks>
 *     <paramref name="SessionId"/> is scoped to <paramref name="UserId"/>
 *     (REQ-CC-3) — a session belonging to a different user is treated as not
 *     found. Revoking the session tied to the caller's current sign-in is
 *     rejected (REQ-SESS-3, <c>CannotRevokeCurrentSession</c>).
 * </remarks>
 */
public record RevokeSessionCommand(int UserId, int SessionId);
