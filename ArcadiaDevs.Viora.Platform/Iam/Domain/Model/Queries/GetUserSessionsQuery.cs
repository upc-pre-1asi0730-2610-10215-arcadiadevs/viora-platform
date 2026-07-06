namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;

/**
 * <summary>
 *     The get-user-sessions query
 * </summary>
 * <remarks>
 *     Returns sessions belonging to <paramref name="UserId"/> only (REQ-SESS-2).
 * </remarks>
 */
public record GetUserSessionsQuery(int UserId);
