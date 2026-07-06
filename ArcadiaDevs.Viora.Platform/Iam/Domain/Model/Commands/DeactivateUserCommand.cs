namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The deactivate-user command
 * </summary>
 * <remarks>
 *     Danger-zone semantics — mirrors OS exactly: this endpoint only ever
 *     deactivates (REQ-DEACT-2). Re-deactivating an already-deactivated user
 *     is a conflict (REQ-DEACT-3).
 * </remarks>
 */
public record DeactivateUserCommand(int UserId);
