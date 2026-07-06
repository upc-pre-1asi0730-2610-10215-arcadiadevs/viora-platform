using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="Subscription" />
///     (REQ-SUB-1..3). Every method returns <see cref="Result{TValue, TError}" />
///     (REQ-CC-3) so <c>SubscriptionsController</c> can map failures via
///     <c>BillingActionResultAssembler</c>.
/// </summary>
public interface ISubscriptionCommandService
{
    /// <summary>Creates a new subscription for a user (REQ-SUB-1).</summary>
    Task<Result<Subscription, Error>> Handle(CreateSubscriptionCommand command, CancellationToken cancellationToken = default);

    /// <summary>Cancels a user's subscription (REQ-SUB-2).</summary>
    Task<Result<Subscription, Error>> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Switches a user's subscription to a different plan (REQ-SUB-3).
    ///     Internal-only — not wired to any controller in this slice.
    /// </summary>
    Task<Result<Subscription, Error>> Handle(SwitchPlanCommand command, CancellationToken cancellationToken = default);
}