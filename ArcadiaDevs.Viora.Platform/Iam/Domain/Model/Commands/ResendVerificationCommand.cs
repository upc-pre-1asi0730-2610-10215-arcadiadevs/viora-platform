namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The resend-verification command
 * </summary>
 * <remarks>
 *     Issues a fresh <c>VerificationToken</c> for an unverified account
 *     (REQ-EV-3). Diverges from OS's <c>email</c> field: WA's login
 *     identifier stays <c>Username</c> (unchanged per proposal), the
 *     account's stored <c>Email</c> is used internally to resend.
 * </remarks>
 */
public record ResendVerificationCommand(string Username);
