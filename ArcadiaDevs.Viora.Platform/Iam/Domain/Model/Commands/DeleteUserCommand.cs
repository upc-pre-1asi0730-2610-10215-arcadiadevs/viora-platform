namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     Command to permanently delete a user's account: the Iam user, their
 *     sessions and verification tokens, and their profile. Backs the
 *     Settings › Security "Delete account" action (Danger zone). Matches OS's
 *     <c>DeleteUserCommand</c> exactly — no self-only/ownership guard on
 *     <paramref name="UserId" />, same inherited-risk idiom as
 *     <c>ChangePasswordCommand</c>/<c>DeactivateUserCommand</c>.
 * </summary>
 */
public record DeleteUserCommand(int UserId);
