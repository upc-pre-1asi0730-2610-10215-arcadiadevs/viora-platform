namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The assign role command
 * </summary>
 * <remarks>
 *     This command object includes the user id and the role name to assign
 * </remarks>
 */
public record AssignRoleCommand(int UserId, string RoleName);
