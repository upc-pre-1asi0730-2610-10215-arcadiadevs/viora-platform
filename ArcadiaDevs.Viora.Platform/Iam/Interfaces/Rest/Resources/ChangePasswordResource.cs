namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The change-password resource
 * </summary>
 * <remarks>
 *     This resource represents the data required to change a user's password:
 *     the current password (for verification) and the new password to apply.
 * </remarks>
 */
public record ChangePasswordResource(string CurrentPassword, string NewPassword);
