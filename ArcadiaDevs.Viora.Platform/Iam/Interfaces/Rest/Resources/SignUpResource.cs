namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

/**
 * <summary>
 *     The sign-up resource
 * </summary>
 * <remarks>
 *     This resource represents the data required to sign up a new user.
 *     Role is optional and defaults to "Grower" when omitted or blank,
 *     mirroring OS's SignUpResource.role + Role.getDefaultRole() contract.
 *     ReferralCode is optional (REQ-REF-6) — an unknown/invalid code never
 *     blocks signup, it is simply a no-op for reward-granting purposes.
 * </remarks>
 */
public record SignUpResource(
    string Username,
    string Password,
    string Email,
    string FullName,
    string? Role = null,
    string? ReferralCode = null,
    string? Phone = null);