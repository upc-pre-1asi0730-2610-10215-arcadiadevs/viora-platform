using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;

/// <summary>
///     The Subscription aggregate root — a user's billing relationship with a
///     <see cref="Plan" /> (REQ-SUB-1..5). <see cref="UserId" /> is ctor-only
///     immutable and FK-validated by the command service via
///     <c>IIamContextFacade</c> before construction (REQ-CC-2), never
///     re-validated afterwards.
/// </summary>
/// <remarks>
///     <see cref="Status" /> starts at <c>PENDING</c> and advances through a
///     self-guarded sequence, mirroring <c>TreatmentPrescription</c>/
///     <c>ServiceProposal</c>'s established <see cref="Result{TValue, TError}" />-
///     returning self-guard convention (design's Per-Aggregate Design table):
///     <see cref="Activate" /> (<c>PENDING</c> → <c>ACTIVE</c>),
///     <see cref="Cancel" /> (<c>ACTIVE</c>-only → <c>CANCELED</c>),
///     <see cref="SwitchTo" /> (<c>ACTIVE</c>-only, updates
///     <see cref="PlanCode" />/<see cref="Interval" />, stays <c>ACTIVE</c>),
///     <see cref="Renew" /> (<c>ACTIVE</c>-only, updates
///     <see cref="CurrentPeriodEnd" />). All four guards use the identical
///     "must currently be ACTIVE" check except <see cref="Activate" />, which
///     requires <c>PENDING</c> — this resolves the design table's "guard:
///     only ACTIVE" wording for <see cref="Cancel" /> over the spec's looser
///     "not already CANCELED" prose (REQ-SUB-2), so canceling a still-
///     <c>PENDING</c> subscription also 409s, not just a re-cancel.
/// </remarks>
public class Subscription
{
    public int Id { get; }

    public int UserId { get; }

    /// <summary>Catalog code of the current plan (mutable via <see cref="SwitchTo" />).</summary>
    public string PlanCode { get; private set; }

    /// <summary>Billing cadence of the current plan (mutable via <see cref="SwitchTo" />).</summary>
    public PlanInterval Interval { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    /// <summary>End of the current billing period (mutable via <see cref="Renew" />).</summary>
    public DateTimeOffset CurrentPeriodEnd { get; private set; }

    private Subscription()
    {
        PlanCode = string.Empty;
    }

    public Subscription(int userId, string planCode, PlanInterval interval, DateTimeOffset currentPeriodEnd)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be positive.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(planCode))
        {
            throw new ArgumentException("Plan code is required.", nameof(planCode));
        }

        UserId = userId;
        PlanCode = planCode;
        Interval = interval;
        CurrentPeriodEnd = currentPeriodEnd;
        Status = SubscriptionStatus.PENDING;
    }

    /// <summary>
    ///     Activates the subscription (first successful payment). Self-guarded
    ///     — only succeeds from <c>PENDING</c>.
    /// </summary>
    public Result<Unit, Error> Activate()
    {
        if (Status != SubscriptionStatus.PENDING)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.ConflictError);
        }

        Status = SubscriptionStatus.ACTIVE;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Cancels the subscription (REQ-SUB-2). Self-guarded — only succeeds
    ///     from <c>ACTIVE</c>; already-<c>CANCELED</c> (or still-<c>PENDING</c>)
    ///     returns a <see cref="BillingErrors.ConflictError" /> (409).
    /// </summary>
    public Result<Unit, Error> Cancel()
    {
        if (Status != SubscriptionStatus.ACTIVE)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.ConflictError);
        }

        Status = SubscriptionStatus.CANCELED;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Switches the subscribed plan (REQ-SUB-3, internal-only — never
    ///     invoked directly via a public endpoint). Self-guarded — only
    ///     succeeds from <c>ACTIVE</c>; a <c>CANCELED</c> subscription MUST
    ///     reactivate via a fresh checkout instead.
    /// </summary>
    public Result<Unit, Error> SwitchTo(string planCode, PlanInterval interval)
    {
        if (Status != SubscriptionStatus.ACTIVE)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.ConflictError);
        }

        PlanCode = planCode;
        Interval = interval;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Renews the current billing period (invoked from payment-webhook
    ///     reconciliation, WU6). Self-guarded — only succeeds from
    ///     <c>ACTIVE</c>.
    /// </summary>
    public Result<Unit, Error> Renew(DateTimeOffset periodEnd)
    {
        if (Status != SubscriptionStatus.ACTIVE)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.ConflictError);
        }

        CurrentPeriodEnd = periodEnd;
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
