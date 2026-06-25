namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;

/**
 * <summary>
 *     The get user by username query
 * </summary>
 * <remarks>
 *     This query object includes the username to search
 * </remarks>
 */
public record GetUserByUsernameQuery(string Username);
