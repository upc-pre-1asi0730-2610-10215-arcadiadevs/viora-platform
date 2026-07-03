namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The sign up command
 * </summary>
 * <remarks>
 *     This command object includes the username and password to sign up,
 *     plus an optional role name. When omitted or blank, the handler
 *     defaults the assigned role to "Grower" (mirrors OS's
 *     Role.getDefaultRole() -> ROLE_GROWER).
 * </remarks>
 */
public record SignUpCommand(string Username, string Password, string Email, string FullName, string? Role = null);
