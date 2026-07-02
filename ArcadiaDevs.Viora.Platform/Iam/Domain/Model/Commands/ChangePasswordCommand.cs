namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The change password command
 * </summary>
 * <remarks>
 *     Mirrors OS's <c>ChangePasswordCommand(Long userId, String currentPassword, Password newPassword)</c>.
 *     The current password MUST be verified against the stored hash before the
 *     new password is applied.
 * </remarks>
 */
public record ChangePasswordCommand(int UserId, string CurrentPassword, string NewPassword);
