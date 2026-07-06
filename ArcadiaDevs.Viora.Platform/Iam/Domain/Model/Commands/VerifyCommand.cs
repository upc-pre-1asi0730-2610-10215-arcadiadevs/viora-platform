namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The verify command
 * </summary>
 * <remarks>
 *     Consumes a verification token and marks the owning user's account as
 *     verified (REQ-EV-2). On success the caller is auto signed-in — matches
 *     OS's behavior; unlike sign-in, this does NOT record a new
 *     <c>UserSession</c> (only <c>SignInCommand</c> records sessions).
 * </remarks>
 */
public record VerifyCommand(string Token);
