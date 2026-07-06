namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The sign in command
 * </summary>
 * <remarks>
 *     This command object includes the username and password to sign in.
 *     <paramref name="UserAgent"/> is captured from the request's
 *     <c>User-Agent</c> header (REQ-SESS-1) — <c>null</c> when absent, the
 *     handler substitutes a placeholder value.
 * </remarks>
 */
public record SignInCommand(string Username, string Password, string? UserAgent = null);
