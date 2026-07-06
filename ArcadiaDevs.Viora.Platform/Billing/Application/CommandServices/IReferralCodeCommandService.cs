using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="ReferralCode" />
///     (REQ-REF-1..3). Returns <see cref="Result{TValue, TError}" />
///     (REQ-CC-3) so callers can map failures via
///     <c>BillingActionResultAssembler</c>.
/// </summary>
public interface IReferralCodeCommandService
{
    /// <summary>
    ///     Gets (or creates, on first access) the referral code for a user
    ///     (REQ-REF-1). Idempotent.
    /// </summary>
    Task<Result<ReferralCode, Error>> Handle(GetOrCreateForUserCommand command, CancellationToken cancellationToken = default);
}