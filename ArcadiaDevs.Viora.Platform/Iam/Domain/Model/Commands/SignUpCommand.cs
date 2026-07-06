namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;

/**
 * <summary>
 *     The sign up command
 * </summary>
 * <remarks>
 *     This command object includes the username and password to sign up,
 *     plus an optional role name. When omitted or blank, the handler
 *     defaults the assigned role to "Grower" (mirrors OS's
 *     Role.getDefaultRole() -> ROLE_GROWER). Also carries an optional
 *     <c>ReferralCode</c> (REQ-REF-6) — nullable and backward-compatible,
 *     existing signups without a referral code keep working unchanged. An
 *     unknown/invalid code never blocks signup; see
 *     <c>Billing.Interfaces.Acl.IBillingContextFacade.GrantReferralRewardToCodeOwner</c>.
 * </remarks>
 */
public record SignUpCommand(
    string Username,
    string Password,
    string Email,
    string FullName,
    string? Role = null,
    string? ReferralCode = null,
    string? Phone = null);